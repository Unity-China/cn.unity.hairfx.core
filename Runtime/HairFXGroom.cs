using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HairFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu("HairFX/HairFX Groom")]
    [ExecuteInEditMode]
    public class HairFXGroom : MonoBehaviour
    {
        public HairFXProfile HairProfile;
        // this option require hair simulation at run mode
        public bool useRenderTexture;

        public HairFXHairObject[] hairObjects { get => m_hairObjects; }
        private HairFXHairObject[] m_hairObjects;

        public List<HairFXObjectDescription> hairObjectDescriptions { get => m_hairObjectDescriptions; }
        private List<HairFXObjectDescription> m_hairObjectDescriptions;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh hairMesh;
        private SubMeshDescriptor[] hairSubMeshDescriptors;
        private Vector3[] hairMeshVertices;
        private Vector2[] hairMeshUVs;
        private Vector2[] hairMeshUV2;
        private Vector2[] hairMeshUV3;
        private int[] hairMeshTriangles;

        public Bounds HairBounds = new Bounds { center = Vector3.zero, extents = new Vector3(1f, 1f, 1f) };
        private Bounds m_WorldBounds;

        public bool needToReload = false;

        private HairFXSimulation hairSimulation;

        private void InitializeHairObjects()
        {
            if (HairProfile == null) return;

            m_hairObjectDescriptions = HairProfile.hairList;
            m_hairObjects = new HairFXHairObject[HairProfile.hairList.Count];

            // get hair simulation
            hairSimulation = GetComponent<HairFXSimulation>();

            // initialize hair objects
            for (int i = 0; i < m_hairObjectDescriptions.Count; i++)
            {
                if (m_hairObjectDescriptions[i].hairAsset != null)
                {
                    // only use render texture at run mode and hair simulation is attached
                    if (Application.isPlaying)
                        m_hairObjects[i] = new HairFXHairObject(m_hairObjectDescriptions[i], HairProfile, useRenderTexture);
                    else
                        m_hairObjects[i] = new HairFXHairObject(m_hairObjectDescriptions[i], HairProfile, false);
                    // initialize material block params
                    m_hairObjects[i].InitializeMaterialParams();
                }
            }

            // update simulation
            if (hairSimulation)
            {
                hairSimulation.InitializeHairSimulation();
                hairSimulation.UpdateConstants();
            }

            // Calculate BoundingBox
            CalculateBoundingBox();
        }

        private void DestroyHairObjects()
        {
            if (m_hairObjects == null) return;

            // release hair objects
            foreach (var hairObject in m_hairObjects)
            {
                if (hairObject == null) continue;

                hairObject.ReleaseBuffers();
            }

            m_hairObjects = null;
        }

        private void InitializeHairMesh()
        {
            if (m_hairObjects == null) return;

            // count triangle length
            int hairMeshTrianglesLength = 0;
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null) continue;
                hairMeshTrianglesLength += m_hairObjects[i].hairMeshTriangles.Length;
            }
            hairMeshTriangles = new int[hairMeshTrianglesLength];

            // fill triangle array
            int count = 0;
            int maxIndice = 0;
            int uvCount = 0;
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null) continue;
                // set base vertex
                m_hairObjects[i].baseVertex = uvCount;
                // get max indice
                maxIndice = 0;
                for (int j = 0; j < m_hairObjects[i].hairMeshTriangles.Length; j++)
                {
                    int indice = m_hairObjects[i].hairMeshTriangles[j];
                    hairMeshTriangles[count++] = indice;
                    maxIndice = Mathf.Max(maxIndice, indice);
                }
                m_hairObjects[i].maxIndice = maxIndice;
                uvCount += maxIndice + 1;
            }

            // mesh uv and vertices
            hairMeshVertices = new Vector3[uvCount];
            hairMeshUVs = new Vector2[uvCount];
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null) continue;
                for(int j = 0; j < m_hairObjects[i].maxIndice; j++)
                {
                    int segmentPerStrand = m_hairObjects[i].renderVertexPerStrand;
                    hairMeshUVs[m_hairObjects[i].baseVertex + j] = new Vector2(j & 1, 1 - (float)((int)(j / 2) % segmentPerStrand) / segmentPerStrand);
                }
            }

            // UV2 for hair per strand texcoord
            hairMeshUV2 = new Vector2[uvCount];
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null || m_hairObjects[i].hairTexCoords == null) continue;
                for (int j = 0; j < m_hairObjects[i].maxIndice; j++)
                {
                    int strandId = j / 2 / m_hairObjects[i].renderVertexPerStrand;
                    hairMeshUV2[m_hairObjects[i].baseVertex + j] = m_hairObjects[i].strandUV[strandId];
                }
            }

            // UV3 for noise per strand
            hairMeshUV3 = new Vector2[uvCount];
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null) continue;
                for (int j = 0; j < m_hairObjects[i].maxIndice; j++)
                {
                    int strandId = j / 2 / m_hairObjects[i].renderVertexPerStrand;
                    hairMeshUV3[m_hairObjects[i].baseVertex + j] = new Vector2(strandId, Mathf.PerlinNoise(strandId * 33.3f, strandId * 77.7f));
                }
            }

            // build mesh
            hairMesh = new Mesh();
            hairMesh.indexFormat = IndexFormat.UInt32;
            hairMesh.vertices = hairMeshVertices;
            hairMesh.uv = hairMeshUVs;
            hairMesh.uv2 = hairMeshUV2;
            hairMesh.uv3 = hairMeshUV3;
            hairMesh.triangles = hairMeshTriangles;
            hairMesh.name = gameObject.name + " Mesh";

            // add sub mesh descriptor
            hairSubMeshDescriptors = new SubMeshDescriptor[m_hairObjects.Length];
            hairMesh.subMeshCount = m_hairObjects.Length;
            int indexStart = 0;
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null) continue;
                hairSubMeshDescriptors[i] = new SubMeshDescriptor(indexStart, m_hairObjects[i].hairMeshTriangles.Length);
                hairSubMeshDescriptors[i].baseVertex = m_hairObjects[i].baseVertex;
                indexStart += m_hairObjects[i].hairMeshTriangles.Length;
            }
            hairMesh.SetSubMeshes(hairSubMeshDescriptors);
            hairMesh.bounds = new Bounds(new Vector3(0, 1f, 0), new Vector3(5f, 5f, 5f));

            // set MeshFilter and MeshRenderer
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if(meshFilter != null)
            {
                meshFilter.mesh = hairMesh;
            }
            if(meshRenderer != null)
            {
                for (int i = 0; i < m_hairObjects.Length; i++)
                {
                    if (m_hairObjects[i] == null) continue;
                    if (i < meshRenderer.sharedMaterials.Length)
                        meshRenderer.SetPropertyBlock(m_hairObjects[i].materialPropertyBlock, i);
                }
            }
        }


        private void LateUpdate()
        {
            if (m_hairObjects == null) return;

            // update mesh bounds
            m_WorldBounds = TransformBoundsForMesh(HairBounds);
            hairMesh.bounds = m_WorldBounds;

            // update hair objects
            for (int i = 0; i < m_hairObjects.Length; i++)
            {
                if (m_hairObjects[i] == null || i >= meshRenderer.sharedMaterials.Length) continue;

                // camera distance and LOD
                float distance = (Camera.main.transform.position - transform.TransformPoint(m_WorldBounds.center)).magnitude;
                m_hairObjects[i].UpdateRenderingParameters(distance);
                hairSubMeshDescriptors[i].indexCount = (int)(m_hairObjects[i].LODHairDensity * m_hairObjects[i].hairMeshTriangles.Length);
                hairMesh.SetSubMesh(i, hairSubMeshDescriptors[i], MeshUpdateFlags.DontRecalculateBounds);

                // set PropertyBlock
                meshRenderer.SetPropertyBlock(m_hairObjects[i].materialPropertyBlock, i);
            }
        }

        public void ReloadHairObjects()
        {
            // Do not initialize anything if the game object is disabled.
            // Otherwise, no cleanup will occur.
            if (isActiveAndEnabled)
            {
                DestroyHairObjects();
                InitializeHairObjects();
                InitializeHairMesh();
            }
        }

        private void OnEnable()
        {
            InitializeHairObjects();
            InitializeHairMesh();
        }

        private void OnDisable()
        {
            DestroyHairObjects();
        }

        private void OnDestroy()
        {
            DestroyHairObjects();
        }


        #region BoundingBox
        private void CalculateBoundingBox()
        {
            Vector3 minXYZ = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxXYZ = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < m_hairObjectDescriptions.Count; i++)
            {
                if (m_hairObjectDescriptions[i].hairAsset != null)
                {
                    foreach (var pos in m_hairObjects[i].initialHairPositions)
                    {
                        minXYZ.x = Mathf.Min(minXYZ.x, pos.x);
                        minXYZ.y = Mathf.Min(minXYZ.y, pos.y);
                        minXYZ.z = Mathf.Min(minXYZ.z, pos.z);
                        maxXYZ.x = Mathf.Max(maxXYZ.x, pos.x);
                        maxXYZ.y = Mathf.Max(maxXYZ.y, pos.y);
                        maxXYZ.z = Mathf.Max(maxXYZ.z, pos.z);
                    }
                }
            }
            HairBounds = new Bounds((minXYZ + maxXYZ) / 2, (maxXYZ - minXYZ) * 1.2f);
        }

        // This is the axis-aligned bounding box of the mesh in its local space
        public Bounds TransformBoundsForMesh(Bounds bounds)
        {
            Vector3 center, extents, axisX, axisY, axisZ;
            if (hairSimulation != null && hairSimulation.parentTransform != null)
            {
                Matrix4x4 mat = transform.worldToLocalMatrix * hairSimulation.globalTransformMatrix;

                center = mat.MultiplyPoint(bounds.center);
                extents = bounds.extents;
                axisX = mat.MultiplyVector(new Vector3(extents.x, 0, 0));
                axisY = mat.MultiplyVector(new Vector3(0, extents.y, 0));
                axisZ = mat.MultiplyVector(new Vector3(0, 0, extents.z));
            }
            else
            {
                center = bounds.center;
                extents = bounds.extents;
                axisX = new Vector3(extents.x, 0, 0);
                axisY = new Vector3(0, extents.y, 0);
                axisZ = new Vector3(0, 0, extents.z);
            }
            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        #endregion
    }
}