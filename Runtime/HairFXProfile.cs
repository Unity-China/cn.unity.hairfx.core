
using System.Collections.Generic;
using UnityEngine;

namespace HairFX
{
    // Order reference: https://blog.redbluegames.com/guide-to-extending-unity-editors-menus-b2de47a746db
    // Note: Requires to restart the Editor after changed the order value
    [CreateAssetMenu(fileName = "HairFX Profile", menuName = "HairFX Profile", order = 201)]
    public class HairFXProfile : ScriptableObject
    {
        // Hairs
        public List<HairFXObjectDescription> hairList;

        // Global Hair Parameters with default settings
        public HairFXSimulationSettings globalSimulationSettings = new HairFXSimulationSettings();
        public HairFXRenderingSettings globalRenderingSettings = new HairFXRenderingSettings();

        // Counts on hair data
        private uint m_NumTotalStrands;
        private uint m_NumTotalVertices;
        private uint m_NumVerticesPerStrand;
        private uint m_NumGuideStrands;
        private uint m_NumGuideVertices;

        public bool hairStatusFoldout;
        public bool strandsSettingFoldout;
        public bool geometrySettingFoldout;
        public bool simulationSettingFoldout;

        public bool needToReload = false;

        public void LoadAllHairDataHeader()
        {
            Clear();

            //string path;
            foreach (var hair in hairList)
            {
                if (hair.hairAsset)
                {
                    m_NumGuideStrands       += (uint)hair.hairAsset.numGuideStrands;
                    m_NumTotalStrands       += (uint)hair.hairAsset.numTotalStrands; // Until we call GenerateFollowHairs, the number of total strands is equal to the number of guide strands. 
                    m_NumVerticesPerStrand  = (uint)hair.hairAsset.numVerticesPerStrand;
                    m_NumGuideVertices      += (uint)hair.hairAsset.numGuideVertices;
                    m_NumTotalVertices      += (uint)hair.hairAsset.numTotalVertices; // Again, the total number of vertices is equal to the number of guide vertices here. 
                }
            }
        }

        public uint getNumGuideStrands()
        {
            return m_NumGuideStrands;
        }
        public uint getNumGuideVertices()
        {
            return m_NumGuideVertices;
        }
        public uint getNumTotalStrands()
        {
            return m_NumTotalStrands;
        }

        void Clear()
        {
            m_NumTotalStrands       = 0;
            m_NumTotalVertices      = 0;
            m_NumVerticesPerStrand  = 0;
            m_NumGuideStrands       = 0;
            m_NumGuideVertices      = 0;
        }
    }
}