//#define GPU_INSTANCING
using System;
using UnityEngine;
using System.Collections;

namespace HairFX
{
    /// <summary>
    ///     Unity Hair simulation implementation.
    ///     Uses the Simulation shader to simulate the hair strands.
    /// </summary>
    [RequireComponent(typeof(HairFXGroom))]
    [AddComponentMenu("HairFX/HairFX Simulation")]
    [ExecuteInEditMode]
    public class HairFXSimulation : MonoBehaviour
    {
        public ComputeShader simulationShader;
        public float frameLimit = 200;
        public bool enableSimulation = true;
        public Transform parentTransform;

        // prevent hair from moving too fast
        [Range(0.05f, 1f)]
        public float speedLimit = 0.5f;

        // reset hair when entering another shot
        [Range(1f, 20f)]
        public float resetDistance = 10f;

        // Colliders
        public bool collision = true;
        public CapsuleCollider[] capsuleColliders;
        private float[] centerAndRadius0 = new float[TRESSFX_MAX_NUM_COLLISION_CAPSULES * 4];
        private float[] centerAndRadius1 = new float[TRESSFX_MAX_NUM_COLLISION_CAPSULES * 4];

        // Wind
        public Vector3 windDirection;
        public float windMagnitude;
        public float windConeAngle = 40f;
        public float pulseMagnitude;
        public float pulseFrequency = 0.01f;
        public float windTurbulence;
        public bool useWindZone = true;
        public WindZone windZone;
        private Vector4 windForce1;
        private Vector4 windForce2;
        private Vector4 windForce3;
        private Vector4 windForce4;

        // Debug
        public bool doIntegrationAndGlobalShapeConstraints = true;
        public bool doVSP = true;
        public bool doLocalShapeConstraints = true;
        public bool doLengthConstraintsWindAndCollision = true;
        public bool followHairs = true;

        // transform
        public Matrix4x4 globalTransformMatrix;

        // reset transform
        private Matrix4x4 resetDistanceMatrix;
        private Matrix4x4 lastFrameTransformMatrix;

        // last frame pos
        private Vector3 lastFramePosition; 

        private const int THREAD_GROUP_SIZE = 64;
        private const int TESSELATION_THREAD_GROUP_SIZE = 128;
        private const int TRESSFX_MAX_NUM_COLLISION_CAPSULES = 8;

        private HairFXGroom hairAssetsGroup;

        private bool m_firstUpdate = true;
        private float m_lastTimeSimulated;

        private HairFXSimulationSettings localSettings;
        private HairFXSimulationSettings globalSettings;

        private int InitializePositionsKernelId;
        private int TransformToWolrdKernelId;
        private int ResetWhenTravelFarDistanceKernelId;
        private int TessellationKernelId;
        private int IntegrationAndGlobalShapeConstraintsKernelId;
        private int LengthConstraintsWindAndCollisionKernelId;
        private int LocalShapeConstraintsKernelId;
        private int PrepareFollowHairBeforeTurningIntoGuideKernelId;
        private int CalculateStrandLevelDataId;
        private int VelocityShockPropagationId;
        private int UpdateFollowHairVerticesKernelId;
        private int BuffersToTexturesKernelId;

        private Vector4 shape = new Vector4();
        private Vector4 gravTimeTip = new Vector4();
        private Vector2Int simInts = new Vector2Int();
        private Vector3Int counts = new Vector3Int();
        private Vector4 VSP_coeff = new Vector4(0.758f, 1.208f, 0, 0);

        private int fixedUpdateTimes = 0;

        #region Initialization

        public void InitializeHairSimulation()
        {
            // Get master
            hairAssetsGroup = GetComponent<HairFXGroom>();

            // find compute shader kernels
            if (simulationShader != null)
            {
                // Get Kernel Ids
                InitializePositionsKernelId = simulationShader.FindKernel("InitializePositions");
                TransformToWolrdKernelId = simulationShader.FindKernel("TransformToWolrd");
                ResetWhenTravelFarDistanceKernelId = simulationShader.FindKernel("ResetWhenTravelFarDistance");
                TessellationKernelId = simulationShader.FindKernel("Tessellation");
                IntegrationAndGlobalShapeConstraintsKernelId = simulationShader.FindKernel("IntegrationAndGlobalShapeConstraints");
                LocalShapeConstraintsKernelId = simulationShader.FindKernel("LocalShapeConstraints");
                LengthConstraintsWindAndCollisionKernelId = simulationShader.FindKernel("LengthConstraintsWindAndCollision");
                UpdateFollowHairVerticesKernelId = simulationShader.FindKernel("UpdateFollowHairVertices");
                PrepareFollowHairBeforeTurningIntoGuideKernelId = simulationShader.FindKernel("PrepareFollowHairBeforeTurningIntoGuide");
                CalculateStrandLevelDataId = simulationShader.FindKernel("CalculateStrandLevelData");
                VelocityShockPropagationId = simulationShader.FindKernel("VelocityShockPropagation");
                BuffersToTexturesKernelId = simulationShader.FindKernel("BuffersToTextures");
            }

            // set numOfStrandsPerThreadGroup 
            if (hairAssetsGroup != null && hairAssetsGroup.hairObjects != null)
            {
                foreach (var hairObject in hairAssetsGroup.hairObjects)
                {
                    if (hairObject != null)
                        hairObject.numOfStrandsPerThreadGroup = THREAD_GROUP_SIZE / hairObject.numVerticesPerStrand;
                }
            }

            // set first update
            m_firstUpdate = true;
        }

        public void OnEnable()
        {
            // Get HairFX Assets Group
            hairAssetsGroup = GetComponent<HairFXGroom>();
            simulationShader = Resources.Load<ComputeShader>("HairSimulation");
        }

        public void Start()
        {
            InitializeHairSimulation();
        }

        private void OnValidate()
        {
            InitializeHairSimulation();
        }

        #endregion

        #region Updates

        public void UpdateConstants()
        {
#if UNITY_EDITOR
            // We must update the constants in the Play Mode as well, otherwise the hair does not move or animate.
            if (Application.isPlaying) return;

            if (hairAssetsGroup == null || hairAssetsGroup.hairObjects == null || simulationShader == null)
            {
                return;
            }

            if (parentTransform != null)
            {
                globalTransformMatrix = parentTransform.localToWorldMatrix * Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            }
            else
            {
                globalTransformMatrix = transform.localToWorldMatrix;
            }

            //HairFXHairObject hairObject;
            foreach (var hairObject in hairAssetsGroup.hairObjects)
            {
                if (hairObject == null) continue;

                localSettings = hairObject.localSimulationSettings;
                globalSettings = hairObject.GlobalSimulationSettings;

                // Calculate dispatch counts
                var vertexCount = hairObject.numGuideVertices;
                var numOfGroupsForCS_VertexLevel = (int)(vertexCount / (float)THREAD_GROUP_SIZE * 1); // * 1 = * density

                // Set constant data
                SetConstants(hairObject, localSettings, globalSettings);

                // transform to world
                SetBuffers(TransformToWolrdKernelId, hairObject);
                simulationShader.Dispatch(TransformToWolrdKernelId, numOfGroupsForCS_VertexLevel, 1, 1);

                // tessellation
                if (hairObject.useTessellation)
                {
                    var numOfGroupsForCS_TessellationVertexLevel = (int)(hairObject.numGuideStrands * hairObject.numTessellationPerStrand / (float)TESSELATION_THREAD_GROUP_SIZE * 1);
                    SetBuffers(TessellationKernelId, hairObject);
                    simulationShader.Dispatch(TessellationKernelId, numOfGroupsForCS_TessellationVertexLevel, 1, 1);  // 64 tessellation points per guide strand
                }
                // update follow hairs
                else if (followHairs)
                {
                    SetBuffers(UpdateFollowHairVerticesKernelId, hairObject);
                    simulationShader.Dispatch(UpdateFollowHairVerticesKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                }

            }
#endif
        }

        // this update is for Editor run mode and Build run mode
        public void SimulateHair()
        {
            if (hairAssetsGroup == null || hairAssetsGroup.hairObjects == null || simulationShader == null)
            {
                return;
            }

            // Frame skipping if rendering too fast
            if (Time.time - m_lastTimeSimulated < 1.0f / frameLimit) return;
            m_lastTimeSimulated = Time.time;

            if (parentTransform != null)
            {
                globalTransformMatrix = parentTransform.localToWorldMatrix * Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            }
            else
            {
                globalTransformMatrix = transform.localToWorldMatrix;
            }
            resetDistanceMatrix = globalTransformMatrix * lastFrameTransformMatrix.inverse;

            SimulateWind();
            SetColliders();

            //HairFXHairObject hairObject;
            foreach (var hairObject in hairAssetsGroup.hairObjects)
            {
                if (hairObject == null) continue;

                localSettings = hairObject.localSimulationSettings;
                globalSettings = hairObject.GlobalSimulationSettings;

                // update simulation parameters
                hairObject.UpdateSimulationParameters();

                // Set constant data
                SetConstants(hairObject, localSettings, globalSettings);

                // Calculate dispatch counts
                var vertexCount = hairObject.numGuideVertices;
                var strandCount = hairObject.numGuideStrands;

                var numOfGroupsForCS_VertexLevel = (int)(vertexCount / (float)THREAD_GROUP_SIZE * 1); // * 1 = * density
                var numOfGroupsForCS_StrandLevel = (int)(strandCount / (float)THREAD_GROUP_SIZE * 1);


                // first update initialize positions
                if (m_firstUpdate)
                {
                    SetBuffers(InitializePositionsKernelId, hairObject);
                    simulationShader.Dispatch(InitializePositionsKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                }
                // Reset When Travel A Far Distance
                else if ((lastFramePosition - transform.position).magnitude > resetDistance)
                {
                    SetBuffers(ResetWhenTravelFarDistanceKernelId, hairObject);
                    simulationShader.Dispatch(ResetWhenTravelFarDistanceKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                }
                else
                {
                    // Set buffers to all other kernels
                    SetBuffers(IntegrationAndGlobalShapeConstraintsKernelId, hairObject);
                    SetBuffers(LocalShapeConstraintsKernelId, hairObject);
                    SetBuffers(LengthConstraintsWindAndCollisionKernelId, hairObject);
                    SetBuffers(UpdateFollowHairVerticesKernelId, hairObject);
                    SetBuffers(PrepareFollowHairBeforeTurningIntoGuideKernelId, hairObject);
                    SetBuffers(CalculateStrandLevelDataId, hairObject);
                    SetBuffers(VelocityShockPropagationId, hairObject);

                    // dispatch shaders
                    if (doIntegrationAndGlobalShapeConstraints)
                    {
                        simulationShader.Dispatch(IntegrationAndGlobalShapeConstraintsKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                    }

                    // velocity shock propagation
                    if (doVSP)
                    {
                        simulationShader.Dispatch(CalculateStrandLevelDataId, numOfGroupsForCS_StrandLevel, 1, 1);
                        simulationShader.Dispatch(VelocityShockPropagationId, numOfGroupsForCS_VertexLevel, 1, 1);
                    }

                    // local shape constraints
                    if (doLocalShapeConstraints)
                    {
                        int iterations = hairObject.GetSimulationSettingValue(localSettings.localConstraintsIterations, globalSettings.localConstraintsIterations);
                        for (int iter = 0; iter < iterations; iter++)
                        {
                            simulationShader.Dispatch(LocalShapeConstraintsKernelId, numOfGroupsForCS_StrandLevel, 1, 1);
                        }
                    }

                    // length constraints and collision
                    if (doLengthConstraintsWindAndCollision)
                    {
                        simulationShader.Dispatch(LengthConstraintsWindAndCollisionKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                    }
                }

                // tessellation
                if (hairObject.useTessellation)
                {
                    var numOfGroupsForCS_TessellationVertexLevel = (int)(hairObject.numGuideStrands * hairObject.numTessellationPerStrand / (float)TESSELATION_THREAD_GROUP_SIZE * 1);
                    SetBuffers(TessellationKernelId, hairObject);
                    simulationShader.Dispatch(TessellationKernelId, numOfGroupsForCS_TessellationVertexLevel, 1, 1);  // 64 tessellation points per guide strand
                }
                // update follow hairs
                else if (followHairs)
                {
                    SetBuffers(UpdateFollowHairVerticesKernelId, hairObject);
                    simulationShader.Dispatch(UpdateFollowHairVerticesKernelId, numOfGroupsForCS_VertexLevel, 1, 1);
                }

                // update render texture
                if (hairAssetsGroup.useRenderTexture)
                {
                    SetToTextureBuffers(BuffersToTexturesKernelId, hairObject);
                    simulationShader.Dispatch(BuffersToTexturesKernelId, hairObject.numTotalVertices / THREAD_GROUP_SIZE, 1, 1);
                }
            }

            if (m_firstUpdate)
            {
                m_firstUpdate = false;
            }

            // record last frame global transform
            lastFramePosition = transform.position;
            lastFrameTransformMatrix = globalTransformMatrix;
        }

        public void FixedUpdate()
        {
            fixedUpdateTimes += 1;
        }

        // Simulate once more in LateUpdate to prevent hair transform's offset
        public void LateUpdate()
        {
            // Constants must be updated after the animation step, otherwise the animation data from the last frame is used.
            // See https://docs.unity3d.com/Manual/ExecutionOrder.html
            UpdateConstants();

            while(fixedUpdateTimes > 0)
            {
                SimulateHair();
                fixedUpdateTimes -= 1;
            }
        }

        private void SimulateWind()
        {
            Vector3 direction = windDirection;
            float magnitude = windMagnitude;
            float pulseMag = pulseMagnitude;
            float pulseFreq = pulseFrequency;
            float turbulence = windTurbulence;
            if (useWindZone && windZone != null)
            {
                direction = windZone.transform.position;
                magnitude = windZone.windMain;
                pulseMag = windZone.windPulseMagnitude;
                pulseFreq = windZone.windPulseFrequency;
                turbulence = windZone.windTurbulence;
            }

            // Simulate wind
            //var wM = magnitude * (Mathf.Pow(Mathf.Sin(Time.frameCount * 0.05f), 2.0f) + 0.5f);
            var wM = magnitude + UnityEngine.Random.Range(0, turbulence) + pulseMag * (1.5f - Mathf.Cos(Time.frameCount * pulseFreq));
            var windDirN = direction.normalized;
            var XAxis = new Vector3(1, 0, 0);

            //var rotFromXAxisToWindDir = Quaternion.identity;
            //var xCrossW = Vector3.Cross(XAxis, windDirN);
            //var angle = Mathf.Asin(xCrossW.magnitude);
            //if (angle > 0.001f) rotFromXAxisToWindDir = Quaternion.AngleAxis(angle, xCrossW.normalized);

            var rotFromXAxisToWindDir = Quaternion.FromToRotation(XAxis, windDirN);


            {
                var rotAxis = new Vector3(0, 1.0f, 0);
                var rot = Quaternion.AngleAxis(windConeAngle, rotAxis);
                var newWindDir = rotFromXAxisToWindDir * rot * XAxis;
                windForce1 = new Vector4(newWindDir.x * wM, newWindDir.y * wM, newWindDir.z * wM, Time.frameCount);
            }

            {
                var rotAxis = new Vector3(0, -1.0f, 0);
                var rot = Quaternion.AngleAxis(windConeAngle, rotAxis);
                var newWindDir = rotFromXAxisToWindDir * rot * XAxis;
                windForce2 = new Vector4(newWindDir.x * wM, newWindDir.y * wM, newWindDir.z * wM, Time.frameCount);
            }

            {
                var rotAxis = new Vector3(0, 0, 1.0f);
                var rot = Quaternion.AngleAxis(windConeAngle, rotAxis);
                var newWindDir = rotFromXAxisToWindDir * rot * XAxis;
                windForce3 = new Vector4(newWindDir.x * wM, newWindDir.y * wM, newWindDir.z * wM, Time.frameCount);
            }

            {
                var rotAxis = new Vector3(0, 0, -1.0f);
                var rot = Quaternion.AngleAxis(windConeAngle, rotAxis);
                var newWindDir = rotFromXAxisToWindDir * rot * XAxis;
                windForce4 = new Vector4(newWindDir.x * wM, newWindDir.y * wM, newWindDir.z * wM, Time.frameCount);
            }
        }

        private void SetColliders()
        {
            // capsule colliders
            if (collision && capsuleColliders.Length > 0)
            {
                int colliderIdx = 0;
                foreach (CapsuleCollider capsuleCollider in capsuleColliders)
                {
                    if (capsuleCollider == null) continue;

                    Vector3 capsuleUp;
                    if (capsuleCollider.direction == 0)
                    {
                        capsuleUp = new Vector3(1, 0, 0);
                    }
                    else if (capsuleCollider.direction == 1)
                    {
                        capsuleUp = new Vector3(0, 1, 0);
                    }
                    else
                    {
                        capsuleUp = new Vector3(0, 0, 1);
                    }

                    var scaleVec = Vector3.Scale(capsuleCollider.transform.lossyScale, (Vector3.one - capsuleUp));
                    float colliderScale = Mathf.Max(scaleVec.x, scaleVec.y, scaleVec.z);
                    {
                        Vector3 center0 = capsuleCollider.center + capsuleUp * Mathf.Max(0f, capsuleCollider.height / 2 - capsuleCollider.radius);
                        center0 = capsuleCollider.transform.TransformPoint(center0);
                        float radius0 = capsuleCollider.radius * colliderScale;

                        centerAndRadius0[colliderIdx * 4] = center0.x;
                        centerAndRadius0[colliderIdx * 4 + 1] = center0.y;
                        centerAndRadius0[colliderIdx * 4 + 2] = center0.z;
                        centerAndRadius0[colliderIdx * 4 + 3] = radius0;
                    }
                    {
                        Vector3 center1 = capsuleCollider.center - capsuleUp * Mathf.Max(0f, capsuleCollider.height / 2 - capsuleCollider.radius);
                        center1 = capsuleCollider.transform.TransformPoint(center1);
                        float radius1 = capsuleCollider.radius * colliderScale;

                        centerAndRadius1[colliderIdx * 4] = center1.x;
                        centerAndRadius1[colliderIdx * 4 + 1] = center1.y;
                        centerAndRadius1[colliderIdx * 4 + 2] = center1.z;
                        centerAndRadius1[colliderIdx * 4 + 3] = radius1;
                    }
                    colliderIdx++;
                }

                simulationShader.SetInt("g_numCollisionCapsules", colliderIdx);
                simulationShader.SetFloats("g_centerAndRadius0", centerAndRadius0);
                simulationShader.SetFloats("g_centerAndRadius1", centerAndRadius1);
            }
            else
            {
                simulationShader.SetInt("g_numCollisionCapsules", 0);
            }
        }

        private void SetBuffers(int kernelId, HairFXHairObject hairObject)
        {
            simulationShader.SetBuffer(kernelId, "g_HairVertexPositions", hairObject.dynamicState.positions);
            simulationShader.SetBuffer(kernelId, "g_HairVertexTangents", hairObject.dynamicState.tangents);
            simulationShader.SetBuffer(kernelId, "g_HairVertexPositionsPrev", hairObject.dynamicState.positionsPrev);
            simulationShader.SetBuffer(kernelId, "g_HairVertexPositionsPrevPrev", hairObject.dynamicState.positionsPrevPrev);
            simulationShader.SetBuffer(kernelId, "g_StrandLevelData", hairObject.dynamicState.strandLevelData);
            simulationShader.SetBuffer(kernelId, "g_InitialHairPositions", hairObject.initialHairPositionsBuffer);
            simulationShader.SetBuffer(kernelId, "g_HairRestLengthSRV", hairObject.hairRestLengthSRVBuffer);
            simulationShader.SetBuffer(kernelId, "g_HairStrandType", hairObject.hairStrandTypeBuffer);
            simulationShader.SetBuffer(kernelId, "g_FollowHairRootOffset", hairObject.followHairRootOffsetBuffer);
            simulationShader.SetBuffer(kernelId, "g_TessellatedPositions", hairObject.dynamicState.tessellatedPositions);
            simulationShader.SetBuffer(kernelId, "g_TessellatedTangents", hairObject.dynamicState.tessellatedTangents);
        }

        private void SetToTextureBuffers(int kernelId, HairFXHairObject hairObject)
        {
            simulationShader.SetTexture(kernelId, "g_HairVertexPositionsTexture", hairObject.dynamicState.positionsTexture);
            simulationShader.SetTexture(kernelId, "g_HairVertexTangentsTexture", hairObject.dynamicState.tangentsTexture);
            simulationShader.SetBuffer(kernelId, "g_HairVertexPositions", hairObject.dynamicState.positions);
            simulationShader.SetBuffer(kernelId, "g_HairVertexTangents", hairObject.dynamicState.tangents);
        }

        private void SetConstants(HairFXHairObject hairObject, HairFXSimulationSettings localSettings, HairFXSimulationSettings globalSettings)
        {
            float g_scale = globalTransformMatrix.lossyScale.magnitude / Vector3.one.magnitude;
            // set scale
            simulationShader.SetFloat("g_scale", g_scale);

            // set speed limit
            simulationShader.SetFloat("g_SpeedLimit", speedLimit);

            // set reset distance
            simulationShader.SetFloat("g_ResetDistance", resetDistance);

            // set reset matrix
            simulationShader.SetMatrix("g_ResetDistanceMatrix", resetDistanceMatrix);

            // Set transform values
            simulationShader.SetMatrix("g_ModelTransformForHead", globalTransformMatrix);
            simulationShader.SetFloats("g_ModelRotateForHead", QuaternionToFloatArray(globalTransformMatrix.rotation));

            // Set wind
            simulationShader.SetVector("g_Wind", windForce1);
            simulationShader.SetVector("g_Wind1", windForce2);
            simulationShader.SetVector("g_Wind2", windForce3);
            simulationShader.SetVector("g_Wind3", windForce4);

            // Set damping, local stiffness, global stiffness, global range
            shape.x = hairObject.GetSimulationSettingValue(localSettings.damping, globalSettings.damping);
            //shape.y = hairObject.GetSimulationSettingValue(localSettings.localConstraintStiffness, globalSettings.localConstraintStiffness);
            //shape.z = hairObject.GetSimulationSettingValue(localSettings.globalConstraintStiffness, globalSettings.globalConstraintStiffness);
            //shape.w = hairObject.GetSimulationSettingValue(localSettings.globalConstraintsRange, globalSettings.globalConstraintsRange);
            simulationShader.SetVector("g_Shape", shape);

            // set g_GravTimeTip: g_GravityMagnitude, g_TimeStep, g_TipSeparationFactor
            gravTimeTip.x = hairObject.GetSimulationSettingValue(localSettings.gravityMagnitude, globalSettings.gravityMagnitude);
            gravTimeTip.y = Time.fixedDeltaTime;
            gravTimeTip.z = hairObject.GetSimulationSettingValue(localSettings.tipSeparation, globalSettings.tipSeparation);
            simulationShader.SetVector("g_GravTimeTip", gravTimeTip);

            // set g_SimInts: GetLengthConstraintIterations, GetLocalConstraintIterations
            simInts.x = hairObject.GetSimulationSettingValue(localSettings.lengthConstraintsIterations, globalSettings.lengthConstraintsIterations);
            simInts.y = hairObject.GetSimulationSettingValue(localSettings.localConstraintsIterations, globalSettings.localConstraintsIterations);
            simulationShader.SetInts("g_SimInts", simInts.x, simInts.y);

            // set g_Counts
            counts.x = hairObject.numOfStrandsPerThreadGroup;
            counts.y = hairObject.numFollowHairsPerGuideHair;
            counts.z = hairObject.numVerticesPerStrand;
            simulationShader.SetInts("g_Counts", counts.x, counts.y, counts.z, hairObject.numTessellationPerStrand);

            // set g_VSP
            VSP_coeff.x = hairObject.GetSimulationSettingValue(localSettings.vspCoeff, globalSettings.vspCoeff);
            VSP_coeff.y = hairObject.GetSimulationSettingValue(localSettings.vspAccelThreshold, globalSettings.vspAccelThreshold);
            simulationShader.SetVector("g_VSP", VSP_coeff);

            // clamp delta value
            //simulationShader.SetFloat("g_ClampPositionDelta", hairObject.GetSimulationSettingValue(localSettings.clampPositionDelta, globalSettings.clampPositionDelta));

            // g_NumVerticesPerGuideFollowStrand
            simulationShader.SetInt("g_NumVerticesPerGuideFollowStrand", hairObject.numVerticesPerGuideFollowStrand);

            // resetPositions
            simulationShader.SetFloat("g_ResetPositions", enableSimulation ? 0f : 1f);

            // set stiffness curve
            simulationShader.SetFloats("globalStiffnessCurve", hairObject.globalStiffnessCurve);
            simulationShader.SetFloats("localStiffnessCurve", hairObject.localStiffnessCurve);

            // follow hair length offset
            simulationShader.SetFloat("g_FollowHairLengthOffset", hairObject.GetSimulationSettingValue(localSettings.followHairLengthOffset, globalSettings.followHairLengthOffset));
        }

        #endregion

        #region Utilities

        private float[] MatrixToFloatArray(Matrix4x4 matrix)
        {
            return new[]
            {
                matrix.m00, matrix.m01, matrix.m02, matrix.m03,
                matrix.m10, matrix.m11, matrix.m12, matrix.m13,
                matrix.m20, matrix.m21, matrix.m22, matrix.m23,
                matrix.m30, matrix.m31, matrix.m32, matrix.m33
            };
        }

        private float[] QuaternionToFloatArray(Quaternion quaternion)
        {
            return new[]
            {
                quaternion.x,
                quaternion.y,
                quaternion.z,
                quaternion.w
            };
        }

        #endregion
    }
}