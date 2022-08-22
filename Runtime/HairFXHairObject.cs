using System;
using UnityEngine;

namespace HairFX
{
    // AMD Ref: TressFXHairObject.h #46
    public class HairFXDynamicState
    {
        public void CreateGPUResources(int numStrands, int numGuideStrands, int numVerticesPerGuideFollowStrand, int numTessellationPerStrand, HairFXAsset asset, bool useRenderTexture)
        {
            m_PositionsPrev = Utilities.InitializeBuffer(asset.positions, 16);
            m_PositionsPrevPrev = Utilities.InitializeBuffer(asset.positions, 16);
            m_Positions = Utilities.InitializeBuffer(asset.positions, 16);
            m_Tangents = Utilities.InitializeBuffer(asset.tangents, 16);
            m_TessellationPositions = Utilities.InitializeBuffer(new Vector4[Math.Max(1, numStrands * numTessellationPerStrand)], 16);
            m_TessellatedTangents = Utilities.InitializeBuffer(new Vector4[Math.Max(1, numStrands * numTessellationPerStrand)], 16);

            m_StrandLevelData = Utilities.InitializeBuffer(new HairFXStrandLevelData[numStrands], 48);
            if (useRenderTexture)
            {
                m_PositionsTexture = Utilities.GenerateRenderTextureRGBA(asset.positions, numGuideStrands, numVerticesPerGuideFollowStrand);
                m_TangentsTexture = Utilities.GenerateRenderTextureRGBA(asset.tangents, numGuideStrands, numVerticesPerGuideFollowStrand);
            }
        }

        public ComputeBuffer positions { get => m_Positions; }
        private ComputeBuffer m_Positions;
        public ComputeBuffer tessellatedPositions { get => m_TessellationPositions; }
        private ComputeBuffer m_TessellationPositions;
        public ComputeBuffer tangents { get => m_Tangents; }
        private ComputeBuffer m_Tangents;
        public ComputeBuffer tessellatedTangents { get => m_TessellatedTangents; }
        private ComputeBuffer m_TessellatedTangents;
        public ComputeBuffer positionsPrev { get => m_PositionsPrev; }
        private ComputeBuffer m_PositionsPrev;
        public ComputeBuffer positionsPrevPrev { get => m_PositionsPrevPrev; }
        private ComputeBuffer m_PositionsPrevPrev;
        public ComputeBuffer strandLevelData { get => m_StrandLevelData; }
        private ComputeBuffer m_StrandLevelData;
        public RenderTexture positionsTexture { get => m_PositionsTexture; }
        private RenderTexture m_PositionsTexture;
        public RenderTexture tangentsTexture { get => m_TangentsTexture; }
        private RenderTexture m_TangentsTexture;
    }

    // AMD Ref: TressFXHairObject.h #78
    public struct HairFXStrandLevelData
    {
        Vector4 skinningQuat;
        Vector4 vspQuat;
        Vector4 vspTranslation;
    }

    // AMD Ref: TressFXHairObject.h #85
    public class HairFXHairObject
    {
        #region Properties

        // hair asset information
        public int numTotalVertices { get => m_NumTotalVertices; }
        private int m_NumTotalVertices;
        public int numTotalStrands { get => m_NumTotalStrands; }
        private int m_NumTotalStrands;
        public int numVerticesPerStrand { get => m_NumVerticesPerStrand; }
        private int m_NumVerticesPerStrand;
        public int numOfStrandsPerThreadGroup;
        public int numTessellationPerStrand { get => m_NumTessellationPerStrand; }
        private int m_NumTessellationPerStrand;
        public int numGuideVertices { get => m_NumGuideVertices; }
        private int m_NumGuideVertices;
        public int numGuideStrands { get => m_NumGuideStrands; }
        private int m_NumGuideStrands;
        public int numFollowHairsPerGuideHair { get => m_NumFollowHairsPerGuideHair; }
        private int m_NumFollowHairsPerGuideHair;
        public int numVerticesPerGuideFollowStrand { get => m_NumVerticesPerGuideFollowStrand; }
        private int m_NumVerticesPerGuideFollowStrand;


        // For LOD calculations
        public float LODHairDensity { get => m_LODHairDensity; }
        private float m_LODHairDensity = 1.0f;

        // position and tangent
        private HairFXDynamicState m_DynamicState = new HairFXDynamicState();
        public HairFXDynamicState dynamicState { get => m_DynamicState; }

        public ComputeBuffer initialHairPositionsBuffer { get => m_InitialHairPositionsBuffer; }
        private ComputeBuffer m_InitialHairPositionsBuffer;
        public Vector4[] initialHairPositions { get => m_InitialHairPositions; }
        private Vector4[] m_InitialHairPositions;
        public ComputeBuffer hairRestLengthSRVBuffer { get => m_HairRestLengthSRVBuffer; }
        private ComputeBuffer m_HairRestLengthSRVBuffer;
        public ComputeBuffer hairStrandTypeBuffer { get => m_HairStrandTypeBuffer; }
        private ComputeBuffer m_HairStrandTypeBuffer;
        public ComputeBuffer followHairRootOffsetBuffer { get => m_FollowHairRootOffsetBuffer; }
        private ComputeBuffer m_FollowHairRootOffsetBuffer;
        //public ComputeBuffer boneSkinningDataBuffer { get => m_BoneSkinningDataBuffer; }
        //private ComputeBuffer m_BoneSkinningDataBuffer;

        // SRVs for rendering
        public Texture2D hairTexCoordsTexture { get => m_HairTexCoordsTexture; }
        private Texture2D m_HairTexCoordsTexture;
        public ComputeBuffer hairTexCoords { get => m_HairTexCoords; }
        private ComputeBuffer m_HairTexCoords;
        public Vector2[] strandUV { get => m_StrandUV; }
        private Vector2[] m_StrandUV;


        // index buffer
        public GraphicsBuffer indexBuffer { get => m_IndexBuffer; }
        private GraphicsBuffer m_IndexBuffer;
        public uint totalIndices { get => m_TotalIndices; }
        private uint m_TotalIndices;
        public GraphicsBuffer tessellatedIndexBuffer { get => m_TessellatedIndexBuffer; }
        private GraphicsBuffer m_TessellatedIndexBuffer;
        public uint tessellationIndices { get => m_TessellationIndices; }
        private uint m_TessellationIndices;
        public bool useTessellation { get => m_UseTessellation; }
        private bool m_UseTessellation;


        // indices for renderer
        public GraphicsBuffer renderIndexBuffer { get => m_UseTessellation ? tessellatedIndexBuffer : indexBuffer; }
        public int renderVertexPerStrand { get => m_UseTessellation ? m_NumTessellationPerStrand : numVerticesPerStrand; }

        // indices for hair mesh
        public int[] hairMeshTriangles { get => m_UseTessellation? m_tessellatedIndices : m_triangleIndices; }
        public int[] m_triangleIndices;
        public int[] m_tessellatedIndices;

        // base vertex and max indice for hair mesh
        public int baseVertex { get; set; }
        private int m_baseVertex;
        public int maxIndice { get; set; }
        private int m_maxIndice;


        public MaterialPropertyBlock materialPropertyBlock { get => m_MaterialPropertyBlock; }
        private MaterialPropertyBlock m_MaterialPropertyBlock;


        private bool m_UseRenderTexture;
        private HairFXObjectDescription m_hairObjectDescription;
        private HairFXAsset m_hairAsset;


        public HairFXSimulationSettings localSimulationSettings { get => m_localSimulationSet; }
        private HairFXSimulationSettings m_localSimulationSet;
        public HairFXSimulationSettings GlobalSimulationSettings { get => m_GlobalSimulationSet; }
        private HairFXSimulationSettings m_GlobalSimulationSet;
        public HairFXRenderingSettings localRenderingSettings { get => m_localRenderingSet; }
        private HairFXRenderingSettings m_localRenderingSet;
        public HairFXRenderingSettings globalRenderingSettings { get => m_globalRenderingSet; }
        private HairFXRenderingSettings m_globalRenderingSet;


        public float[] globalStiffnessCurve;
        public float[] localStiffnessCurve;

        #endregion

        #region Initialization

        public HairFXHairObject(HairFXObjectDescription hairObjectDescription, HairFXProfile hairProfile, bool useRenderTexture)
        {
            // get settings
            m_hairObjectDescription = hairObjectDescription;
            m_hairAsset = m_hairObjectDescription.hairAsset;

            m_localSimulationSet = hairObjectDescription.localSimulationSettings;
            m_GlobalSimulationSet = hairProfile.globalSimulationSettings;
            m_localRenderingSet = hairObjectDescription.localRenderingSettings;
            m_globalRenderingSet = hairProfile.globalRenderingSettings;

            // tessellation settings
            m_NumTessellationPerStrand = (int)GetStrandsSettingValue(m_localRenderingSet.tessellationNumber, m_globalRenderingSet.tessellationNumber);
            m_UseTessellation = m_NumTessellationPerStrand > 0;

            // asset load hair data
            LoadHairDataFromAsset();

            // get data from hair asset
            m_NumTotalVertices = m_hairAsset.numTotalVertices;
            m_NumTotalStrands = m_hairAsset.numTotalStrands;
            m_NumGuideVertices = m_hairAsset.numGuideVertices;
            m_NumGuideStrands = m_hairAsset.numGuideStrands;
            m_NumVerticesPerStrand = m_hairAsset.numVerticesPerStrand;
            m_NumVerticesPerGuideFollowStrand = (m_hairAsset.numFollowStrandsPerGuide + 1) * m_NumVerticesPerStrand;

            globalStiffnessCurve = new float[m_NumVerticesPerStrand];
            localStiffnessCurve = new float[m_NumVerticesPerStrand];

            m_DynamicState.CreateGPUResources(m_NumTotalStrands, m_NumGuideStrands, m_NumVerticesPerGuideFollowStrand, m_NumTessellationPerStrand, m_hairAsset, useRenderTexture);
            m_InitialHairPositionsBuffer = Utilities.InitializeBuffer(m_hairAsset.positions, 16);
            m_InitialHairPositions = m_hairAsset.positions;
            m_HairRestLengthSRVBuffer = Utilities.InitializeBuffer(m_hairAsset.restLengths, 4);
            m_HairStrandTypeBuffer = Utilities.InitializeBuffer(m_hairAsset.strandTypes, 4);
            m_FollowHairRootOffsetBuffer = Utilities.InitializeBuffer(m_hairAsset.followRootOffsets, 16);
            //m_BoneSkinningDataBuffer = Utilities.InitializeBuffer(asset, 16);

            m_NumVerticesPerStrand = m_hairAsset.numVerticesPerStrand; // m_NumVerticesPerStrand is assigned twice in AMD TressFX.
            m_NumFollowHairsPerGuideHair = m_hairAsset.numFollowStrandsPerGuide;

            // CreateRenderingGPUResources
            CreateRenderingGPUResources(m_hairAsset);

            // GenerateTextures
            if (useRenderTexture)
                GenerateTextures(m_hairAsset);
            m_UseRenderTexture = useRenderTexture;
        }

        // AMD Ref: HairStrand.cpp #59
        private void LoadHairDataFromAsset()
        {
            m_hairAsset.LoadHairData();
            m_hairAsset.GenerateFollowHairs(
                GetStrandsSettingValue(m_localRenderingSet.followHairCount, m_globalRenderingSet.followHairCount),
                GetStrandsSettingValue(m_localSimulationSet.tipSeparation, m_GlobalSimulationSet.tipSeparation),
                GetStrandsSettingValue(m_localRenderingSet.followHairOffset, m_globalRenderingSet.followHairOffset)
                );
            m_hairAsset.ProcessAsset(m_NumTessellationPerStrand);
            if (GetStrandsSettingValue(m_localRenderingSet.bothEndsImmovable, m_globalRenderingSet.bothEndsImmovable))
                m_hairAsset.ImplementBothEndsImmovable();
            //asset.LoadBoneData();
        }

        private void CreateRenderingGPUResources(HairFXAsset asset)
        {
            m_TotalIndices = (uint)asset.triangleIndices.Length;
            if(m_UseTessellation)
                m_TessellationIndices = (uint)asset.tessellatedIndices.Length;

            if (asset.strandUV != null)
            {
                m_StrandUV = asset.strandUV;
                m_HairTexCoords = Utilities.InitializeBuffer(asset.strandUV, 8);
            }

            m_IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, asset.triangleIndices.Length, 4);
            m_IndexBuffer.SetData(asset.triangleIndices);
            m_triangleIndices = asset.triangleIndices;

            if (m_UseTessellation)
            {
                m_TessellatedIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, asset.tessellatedIndices.Length, 4);
                m_TessellatedIndexBuffer.SetData(asset.tessellatedIndices);
                m_tessellatedIndices = asset.tessellatedIndices;
            }
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }

        // Generate texture, to replace compute/structured buffer
        private void GenerateTextures(HairFXAsset asset)
        {
            if (asset.strandUV != null)
                m_HairTexCoordsTexture = Utilities.GenerateTexture2DRG(asset.strandUV, m_NumGuideStrands, m_NumFollowHairsPerGuideHair + 1);
        }

        public void ReleaseBuffers()
        {
            // in m_DynamicState.CreateGPUResources
            dynamicState.positionsPrev.Release();
            dynamicState.positionsPrevPrev.Release();
            dynamicState.positions.Release();
            dynamicState.tangents.Release();
            dynamicState.strandLevelData.Release();
            dynamicState.tessellatedPositions.Release();
            dynamicState.tessellatedTangents.Release();
            if (dynamicState.positionsTexture != null)
                dynamicState.positionsTexture.Release();
            if (dynamicState.tangentsTexture != null)
                dynamicState.tangentsTexture.Release();

            // in HairFXHairObject()
            initialHairPositionsBuffer.Release();
            hairRestLengthSRVBuffer.Release();
            hairStrandTypeBuffer.Release();
            followHairRootOffsetBuffer.Release();

            // in CreateRenderingGPUResources
            if (hairTexCoords != null)
                hairTexCoords.Release();
            indexBuffer.Release();
            if(tessellatedIndexBuffer != null)
                tessellatedIndexBuffer.Release();

            // in GenerateTextures
            if (hairTexCoordsTexture)
            {
#if UNITY_EDITOR
                Texture2D.DestroyImmediate(hairTexCoordsTexture);
#else
                Texture2D.Destroy(hairTexCoordsTexture);
#endif
            }
        }

        #endregion

        #region Parameters

        public void InitializeMaterialParams()
        {
            // Set shader buffers
            if (m_UseRenderTexture)
            {
                m_MaterialPropertyBlock.SetTexture("g_HairVertexTangentsTexture", m_DynamicState.tangentsTexture);
                m_MaterialPropertyBlock.SetTexture("g_HairVertexPositionsTexture", m_DynamicState.positionsTexture);
                m_MaterialPropertyBlock.SetTexture("g_TexCoordsTexture", m_HairTexCoordsTexture);// can annotate
            }

            // non-texture buffers
            m_MaterialPropertyBlock.SetBuffer("g_TexCoords", m_HairTexCoords);// can annotate

            if (m_UseTessellation)
            {
                m_MaterialPropertyBlock.SetBuffer("g_TessellatedTangents", m_DynamicState.tessellatedTangents);
                m_MaterialPropertyBlock.SetBuffer("g_TessellatedPositions", m_DynamicState.tessellatedPositions);
                m_MaterialPropertyBlock.SetBuffer("g_HairVertexTangents", m_DynamicState.tessellatedTangents);
                m_MaterialPropertyBlock.SetBuffer("g_HairVertexPositions", m_DynamicState.tessellatedPositions);
            }
            else // prevent shader buffer out of boundary error on mobile device
            {
                m_MaterialPropertyBlock.SetBuffer("g_TessellatedTangents", m_DynamicState.tangents);
                m_MaterialPropertyBlock.SetBuffer("g_TessellatedPositions", m_DynamicState.positions);
                m_MaterialPropertyBlock.SetBuffer("g_HairVertexTangents", m_DynamicState.tangents);
                m_MaterialPropertyBlock.SetBuffer("g_HairVertexPositions", m_DynamicState.positions);
            }
            m_MaterialPropertyBlock.SetFloat("_UseTessellation", m_UseTessellation ? 1f : 0f);

            m_MaterialPropertyBlock.SetInt("_StrandIndicesCount", (m_NumVerticesPerStrand - 1) * 6);
            m_MaterialPropertyBlock.SetInt("_NumVerticesPerStrand", m_NumVerticesPerStrand);
            m_MaterialPropertyBlock.SetInt("_NumTessellationPerStrand", m_NumTessellationPerStrand);
            m_MaterialPropertyBlock.SetInt("_FollowHairsCount", m_NumFollowHairsPerGuideHair);
            m_MaterialPropertyBlock.SetInt("g_NumVerticesPerGuideFollowStrand", m_NumVerticesPerGuideFollowStrand);
        }

        public void UpdateRenderingParameters(float Distance, bool ShadowUpdate = false)
        {
            // TODO: override each param
            m_LODHairDensity = 1.0f;
            float FiberRadius = GetGeometrySettingValue(m_localRenderingSet.fiberRadius, m_globalRenderingSet.fiberRadius);
            
            // calculate hair LOD density
            if (GetGeometrySettingValue(m_localRenderingSet.enableHairLOD, m_globalRenderingSet.enableHairLOD))
            {
                Vector2 LODRange = GetGeometrySettingValue(m_localRenderingSet.LODRange, m_globalRenderingSet.LODRange);
                float MinLODDist = LODRange.x;
                float MaxLODDist = LODRange.y;

                if (Distance > MinLODDist)
                {
                    float DistanceRatio = Mathf.Clamp01((Distance - MinLODDist) / Math.Max(MaxLODDist - MinLODDist, 0.00001f));

                    // Lerp: x + s(y-x)
                    float LODWidthMultiplier = GetGeometrySettingValue(m_localRenderingSet.LODWidthMultiplier, m_globalRenderingSet.LODWidthMultiplier);
                    FiberRadius *= Mathf.Lerp(1, LODWidthMultiplier, DistanceRatio);

                    // Lerp: x + s(y-x)
                    float LODPercent = GetGeometrySettingValue(m_localRenderingSet.LODPercent, m_globalRenderingSet.LODPercent);
                    m_LODHairDensity = Mathf.Lerp(1.0f, LODPercent, DistanceRatio);
                }
            }

            m_MaterialPropertyBlock.SetFloat("_HairWidth", FiberRadius);
            m_MaterialPropertyBlock.SetFloat("_FiberRatio", GetGeometrySettingValue(m_localRenderingSet.fiberRatio, m_globalRenderingSet.fiberRatio));
            m_MaterialPropertyBlock.SetFloat("_FiberRatioStart", GetGeometrySettingValue(m_localRenderingSet.fiberRatioStart, m_globalRenderingSet.fiberRatioStart));
            m_MaterialPropertyBlock.SetFloat("_EnableThinTip", GetGeometrySettingValue(m_localRenderingSet.enableThinTip, m_globalRenderingSet.enableThinTip) ? 1f : 0f);
        }

        public void UpdateSimulationParameters()
        {
            AnimationCurve StiffnessCurve = GetSimulationSettingValue(localSimulationSettings.globalStiffnessCurve, GlobalSimulationSettings.globalStiffnessCurve);
            for(int i = 0; i < m_NumVerticesPerStrand; ++i)
            {
                globalStiffnessCurve[i] = Mathf.Clamp01(StiffnessCurve.Evaluate((float)i / (m_NumVerticesPerStrand - 1)));
            }

            StiffnessCurve = GetSimulationSettingValue(localSimulationSettings.localStiffnessCurve, GlobalSimulationSettings.localStiffnessCurve);
            for (int i = 0; i < m_NumVerticesPerStrand; ++i)
            {
                localStiffnessCurve[i] = Mathf.Clamp01(StiffnessCurve.Evaluate((float)i / (m_NumVerticesPerStrand - 1)));
            }
        }

        #endregion

        #region GetSettingValuesWithOverride

        public float GetStrandsSettingValue(FloatHairSetting localVal, FloatHairSetting globalVal)
        {
            if (m_hairObjectDescription.strandsSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public int GetStrandsSettingValue(IntHairSetting localVal, IntHairSetting globalVal)
        {
            if (m_hairObjectDescription.strandsSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public bool GetStrandsSettingValue(BoolHairSetting localVal, BoolHairSetting globalVal)
        {
            if (m_hairObjectDescription.strandsSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public TessellationNumber GetStrandsSettingValue(EnumHairSetting localVal, EnumHairSetting globalVal)
        {
            if (m_hairObjectDescription.strandsSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public float GetGeometrySettingValue(FloatHairSetting localVal, FloatHairSetting globalVal)
        {
            if (m_hairObjectDescription.geometrySettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public int GetGeometrySettingValue(IntHairSetting localVal, IntHairSetting globalVal)
        {
            if (m_hairObjectDescription.geometrySettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public bool GetGeometrySettingValue(BoolHairSetting localVal, BoolHairSetting globalVal)
        {
            if (m_hairObjectDescription.geometrySettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public Vector2 GetGeometrySettingValue(FloatRangeHairSetting localVal, FloatRangeHairSetting globalVal)
        {
            if (m_hairObjectDescription.geometrySettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public float GetSimulationSettingValue(FloatHairSetting localVal, FloatHairSetting globalVal)
        {
            if (m_hairObjectDescription.simulationSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public int GetSimulationSettingValue(IntHairSetting localVal, IntHairSetting globalVal)
        {
            if (m_hairObjectDescription.simulationSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public bool GetSimulationSettingValue(BoolHairSetting localVal, BoolHairSetting globalVal)
        {
            if (m_hairObjectDescription.simulationSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public Vector2 GetSimulationSettingValue(FloatRangeHairSetting localVal, FloatRangeHairSetting globalVal)
        {
            if (m_hairObjectDescription.simulationSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        public AnimationCurve GetSimulationSettingValue(CurveHairSetting localVal, CurveHairSetting globalVal)
        {
            if (m_hairObjectDescription.simulationSettingToggle && localVal.overrideStates) return localVal.value;
            else return globalVal.value;
        }

        #endregion

    }
}
