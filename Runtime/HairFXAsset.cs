//---------------------------------------------------------------------------------------
// Loads and processes TressFX files.
// Inputs are binary files/streams/blobs
// Outputs are raw data that will mostly end up on the GPU.
//-------------------------------------------------------------------------------------
//
// Copyright (c) 2019 Advanced Micro Devices, Inc. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace HairFX
{
    // AMD Ref: TressFXAsset.h
    public class HairFXAsset : ScriptableObject
    {
        // for asset inspector
        public TressFXTFXFileHeader header;
        public String filePath;
        public String fileSize;

        // Hair data from *.tfx
        public Vector4[] positions { get => m_Positions; }
        private Vector4[] m_Positions;
        public Vector2[] strandUV { get => m_StrandUV; }
        private Vector2[] m_StrandUV;
        public Vector4[] tangents { get => m_Tangents; }
        private Vector4[] m_Tangents;
        public Vector4[] followRootOffsets { get => m_FollowRootOffsets; }
        private Vector4[] m_FollowRootOffsets;
        public int[] strandTypes { get => m_StrandTypes; }
        private int[] m_StrandTypes;
        public float[] thicknessCoeffs { get => m_ThicknessCoeffs; }
        private float[] m_ThicknessCoeffs;
        public float[] restLengths { get => m_RestLengths; }
        private float[] m_RestLengths;
        public int[] triangleIndices { get => m_TriangleIndices; }
        private int[] m_TriangleIndices;
        public int[] tessellatedIndices { get => m_TessellatedIndices; }
        private int[] m_TessellatedIndices;

        // Bone skinning data from *.tfxbone
        //public TressFXBoneSkinningData[] m_boneSkinningData;

        public int numTotalStrands { get => m_NumTotalStrands; }
        private int m_NumTotalStrands;
        public int numTotalVertices { get => m_NumTotalVertices; }
        private int m_NumTotalVertices;
        public int numVerticesPerStrand { get => m_NumVerticesPerStrand; }
        private int m_NumVerticesPerStrand;
        public int numTessellationPerStrand { get => m_NumTessellationPerStrand; }
        private int m_NumTessellationPerStrand;
        public int numGuideStrands { get => m_NumGuideStrands; }
        private int m_NumGuideStrands;
        public int numGuideVertices { get => m_NumGuideVertices; }
        private int m_NumGuideVertices;
        public int numFollowStrandsPerGuide { get => m_NumFollowStrandsPerGuide; }
        private int m_NumFollowStrandsPerGuide;


        [SerializeField]
        [HideInInspector]
        private byte[] rawBytes;

        [SerializeField]
        [HideInInspector]
        private float scaleFactor = 1f;

        const int AMD_TRESSFX_VERSION_MAJOR = 4;
        const int TRESSFX_SIM_THREAD_GROUP_SIZE = 64;


        #region LoadAssetData
        public bool LoadHeaderData(string path, float scale = 1f)
        {
            scaleFactor = scale;
            bool readSuccess = false;
            if (File.Exists(path))
            {
                // save file path
                filePath = path;
                // get file size
                long fileSizeBytes = new FileInfo(path).Length;
                string[] suf = { " B", " KB", " MB", " GB" };
                if (fileSizeBytes == 0) fileSize = "0" + suf[0];
                int place = Mathf.Min(Mathf.FloorToInt(Mathf.Log(fileSizeBytes, 1024)), 3);
                int num = Mathf.RoundToInt(fileSizeBytes / Mathf.Pow(1024, place));
                fileSize = (num) + suf[place];

                // read header from file
                using (FileStream ioObject = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    // Save *.tfx file as raw bytes data.
                    rawBytes = new byte[ioObject.Length];
                    ioObject.Read(rawBytes, 0, (int)ioObject.Length);

                    // Load just the head of *.tfx file. No detail.
                    readSuccess = LoadHeaderData(ioObject);
                    ioObject.Close();
                }
            }
            return readSuccess;
        }
        
        // AMD Ref: TressFXAsset.cpp #94
        public bool LoadHeaderData(FileStream ioObject)
        {
            header = new TressFXTFXFileHeader { };

            // read the header
            EI_Seek(ioObject, 0); // make sure the stream pos is at the beginning. 
            EI_Read(ref header, ioObject);
            return true;
        }

        // load hair data from file
        public bool LoadHairData()
        {
            bool readSuccess = false;
            string path = filePath;

            using (Stream ioObject = new MemoryStream(rawBytes))
            {
                // Load just the head of *.tfx file. No detail.
                readSuccess = LoadHairData(ioObject);
                ioObject.Close();
            }

            //Debug.Log("LoadHairData():  " + readSuccess.ToString());

            return readSuccess;
        }

        // AMD Ref: TressFXAsset.cpp #94
        //public bool LoadHairData(FileStream ioObject)
        public bool LoadHairData(Stream ioObject)
        {
            //Debug.Log("LoadHairData(FileStream ioObject)");

            // If the tfx version is lower than the current major version, exit. 
            if (header.version < AMD_TRESSFX_VERSION_MAJOR)
            {
                return false;
            }

            uint numStrandsInFile = header.numHairStrands;

            // We make the number of strands be multiple of TRESSFX_SIM_THREAD_GROUP_SIZE. 
            m_NumGuideStrands = (int)(numStrandsInFile - numStrandsInFile % TRESSFX_SIM_THREAD_GROUP_SIZE) + TRESSFX_SIM_THREAD_GROUP_SIZE;

            m_NumVerticesPerStrand = (int)header.numVerticesPerStrand;

            // Make sure number of vertices per strand is greater than two and less than or equal to
            // thread group size (64). Also thread group size should be a mulitple of number of
            // vertices per strand. So possible number is 4, 8, 16, 32 and 64.
            Assert.IsTrue(m_NumVerticesPerStrand > 2 && m_NumVerticesPerStrand <= TRESSFX_SIM_THREAD_GROUP_SIZE && TRESSFX_SIM_THREAD_GROUP_SIZE % m_NumVerticesPerStrand == 0);

            //Debug.Log("LoadHairData(FileStream ioObject): Assert1");

            m_NumFollowStrandsPerGuide = 0;
            m_NumTotalStrands = m_NumGuideStrands; // Until we call GenerateFollowHairs, the number of total strands is equal to the number of guide strands. 
            m_NumGuideVertices = m_NumGuideStrands * m_NumVerticesPerStrand;
            m_NumTotalVertices = m_NumGuideVertices; // Again, the total number of vertices is equal to the number of guide vertices here. 

            Assert.IsTrue(m_NumTotalVertices % TRESSFX_SIM_THREAD_GROUP_SIZE == 0); // number of total vertices should be multiple of thread group size. 
                                                                                    // This assert is actually redundant because we already made m_NumGuideStrands
                                                                                    // and m_NumTotalStrands are multiple of thread group size. 
                                                                                    // Just demonstrating the requirement for number of vertices here in case 
                                                                                    // you are to make your own loader. 
            //Debug.Log("LoadHairData(FileStream ioObject): Assert2");

            m_Positions = new Vector4[m_NumTotalVertices]; // size of m_positions = number of total vertices * size of each position vector. 
                                                           //m_positions = EI_Malloc(m_NumTotalVertices * sizeof(float) * 4); // size of m_positions = number of total vertices * size of each position vector. 

            // Read position data from the io stream. 
            EI_Seek(ioObject, header.offsetVertexPosition);
            EI_Read(ref m_Positions, numStrandsInFile * m_NumVerticesPerStrand * sizeof(float) * 4, ioObject);// note that the position data in io stream contains only guide hairs. If we call GenerateFollowHairs
                                                                                                              // to generate follow hairs, m_positions will be re-allocated. 

            //Debug.Log("scaleFactor: " + scaleFactor.ToString());
            // scale positions
            for(int i = 0; i < numStrandsInFile * m_NumVerticesPerStrand; ++i)
            {
                m_Positions[i].Scale(new Vector4(scaleFactor, scaleFactor, scaleFactor, 1));
            }

            // We need to make up some strands to fill up the buffer because the number of strands from stream is not necessarily multile of thread size. 
            int numStrandsToMakeUp = (int)(m_NumGuideStrands - numStrandsInFile);
            
            for (int i = 0; i < numStrandsToMakeUp; ++i)
            {
                for (int j = 0; j < m_NumVerticesPerStrand; ++j)
                {
                    // use hairs from start to fill
                    long indexLastVertex = (i % numStrandsInFile) * m_NumVerticesPerStrand + j;
                    long indexVertex = (numStrandsInFile + i) * m_NumVerticesPerStrand + j;
                    m_Positions[indexVertex] = m_Positions[indexLastVertex];
                }
                //mStrandCol[numStrandsInFile + i] = mStrandCol[i % numStrandsInFile];
            }


            m_StrandUV = new Vector2[m_NumTotalStrands]; // If we call GenerateFollowHairs to generate follow hairs, 
                                                         // m_strandUV will be re-allocated. 
            if (header.offsetStrandUV > 0 && header.offsetStrandUV + numStrandsInFile * sizeof(float) * 2 <= ioObject.Length)
            {
                // Read strand UVs
                EI_Seek(ioObject, header.offsetStrandUV);
                EI_Read(ref m_StrandUV, numStrandsInFile * sizeof(float) * 2, ioObject);

                // Fill up the last empty space
                uint indexLastStrand = (numStrandsInFile - 1);

                for (uint i = 0; i < numStrandsToMakeUp; ++i)
                {
                    uint indexStrand = (numStrandsInFile + i);
                    m_StrandUV[indexStrand] = m_StrandUV[indexLastStrand];
                }
            }

            m_FollowRootOffsets = new Vector4[m_NumTotalStrands];

            // We don't need to do this, because new Vector4[] are initially all zero already.
            // Fill m_followRootOffsets with zeros
            //memset(ref m_FollowRootOffsets, 0, (uint)m_NumTotalStrands * sizeof(float) * 4);

            return true;
        }
        #endregion

        #region RuntimeProcess
        // AMD Ref: TressFXAsset.cpp #177
        // This generates follow hairs around loaded guide hairs procedually with random distribution within the max radius input. 
        // Calling this is optional. 
        public bool GenerateFollowHairs(int numFollowHairsPerGuideHair, float tipSeparationFactor, float maxRadiusAroundGuideHair = 0.5f)
        {
            Assert.IsTrue(numFollowHairsPerGuideHair >= 0);

            m_NumFollowStrandsPerGuide = numFollowHairsPerGuideHair;

            // Nothing to do, just exit.
            if (numFollowHairsPerGuideHair == 0)
                return false;

            // Recompute total number of hair strands and vertices with considering number of follow hairs per a guide hair. 
            m_NumTotalStrands = (int)(m_NumGuideStrands * (m_NumFollowStrandsPerGuide + 1));
            m_NumTotalVertices = (int)(m_NumTotalStrands * m_NumVerticesPerStrand);

            // keep the old buffers until the end of this function. 
            Vector4[] positionsGuide = m_Positions;
            Vector2[] strandUVGuide = m_StrandUV;

            // re-allocate all buffers
            m_Positions = new Vector4[m_NumTotalVertices];
            m_StrandUV = new Vector2[m_NumTotalStrands];

            m_FollowRootOffsets = new Vector4[m_NumTotalStrands];

            // If we have failed to allocate buffers, then clear the allocated ones and exit. 
            if (m_Positions.Length == 0 || m_StrandUV.Length == 0 || m_FollowRootOffsets.Length == 0)
            {
                return false;
            }

            // type-cast to tressfx_vec3 to handle data easily. 
            //Vector4[] pos = m_positions;
            //Vector4[] followOffset = m_followRootOffsets;

            // Generate follow hairs
            for (int i = 0; i < m_NumGuideStrands; i++)
            {
                int indexGuideStrand = i * (m_NumFollowStrandsPerGuide + 1);
                int indexRootVertMaster = (int)(indexGuideStrand * m_NumVerticesPerStrand);

                int m = (int)(i * m_NumVerticesPerStrand);
                for (int j = indexRootVertMaster; j < indexRootVertMaster + m_NumVerticesPerStrand; j++, m++)
                {
                    m_Positions[j] = positionsGuide[m];
                }

                m_StrandUV[indexGuideStrand] = strandUVGuide[i];

                m_FollowRootOffsets[indexGuideStrand].Set(0, 0, 0, 0);
                //m_FollowRootOffsets[indexGuideStrand].w = indexGuideStrand;
                //Vector4 v01 = m_positions[indexRootVertMaster + 1] - m_positions[indexRootVertMaster];
                //v01.Normalize();
                Vector4 v01_4 = m_Positions[indexRootVertMaster + 1] - m_Positions[indexRootVertMaster];
                Vector3 v01 = new Vector3(v01_4.x, v01_4.y, v01_4.z);
                v01.Normalize();

                // Find two orthogonal unit tangent vectors to v01
                //Vector4 t0 = Vector4.zero, t1 = Vector4.zero;
                Vector3 t0 = Vector3.zero, t1 = Vector3.zero;
                //GetTangentVectors(v01, t0, t1);
                Utilities.GetTangentVectors(v01, out t0, out t1);
                //perpendicular with each other
                t1 = Vector3.Cross(v01, t0);
                t1.Normalize();
                t0.Normalize();
                for (int j = 0; j < m_NumFollowStrandsPerGuide; j++)
                {
                    int indexStrandFollow = indexGuideStrand + j + 1;
                    int indexRootVertFollow = (int)(indexStrandFollow * m_NumVerticesPerStrand);

                    m_StrandUV[indexStrandFollow] = m_StrandUV[indexGuideStrand];

                    // offset vector from the guide strand's root vertex position
                    //Vector4 offset = GetRandom(-maxRadiusAroundGuideHair, maxRadiusAroundGuideHair) * t0 + GetRandom(-maxRadiusAroundGuideHair, maxRadiusAroundGuideHair) * t1;
                    Vector3 offset = Utilities.GetRandom(-maxRadiusAroundGuideHair, maxRadiusAroundGuideHair) * t0 +
                        Utilities.GetRandom(-maxRadiusAroundGuideHair, maxRadiusAroundGuideHair) * t1;
                    m_FollowRootOffsets[indexStrandFollow] = offset;
                    m_FollowRootOffsets[indexStrandFollow].w = Utilities.GetRandom(0, 1);

                    for (int k = 0; k < m_NumVerticesPerStrand; k++)
                    {
                        float factor = tipSeparationFactor * ((float)k / ((float)m_NumVerticesPerStrand)) + 1.0f;
                        //m_positions[indexRootVertFollow + 1] = m_positions[indexRootVertMaster + k] + offset * factor;
                        //m_positions[indexRootVertFollow + 1].w = m_positions[indexRootVertMaster + k].w;
                        Vector3 followPos = new Vector3(
                            m_Positions[indexRootVertMaster + k].x,
                            m_Positions[indexRootVertMaster + k].y,
                            m_Positions[indexRootVertMaster + k].z);
                        followPos += offset * factor;
                        m_Positions[indexRootVertFollow + k] = new Vector4(
                            followPos.x, followPos.y, followPos.z,
                            m_Positions[indexRootVertMaster + k].w);
                    }
                }
            }
            return true;
        }

        public void ImplementBothEndsImmovable()
        {
            // if set TwoSideLocked, set vertex unmovable.
            for (int index = 0; index < m_NumTotalVertices; index += m_NumVerticesPerStrand)
            {
                m_Positions[index].w = 0;
                m_Positions[index + 1].w = 0;
                m_Positions[index + m_NumVerticesPerStrand - 1].w = 0;
                // last one, first and second should be fixed
            }
        }
        #endregion
        
        #region ProcessAsset
        // AMD Ref: TressFXAsset.cpp #250
        public bool ProcessAsset(int tessellationNum)
        {
            m_NumTessellationPerStrand = tessellationNum;

            m_StrandTypes = new int[m_NumTotalStrands];
            m_Tangents = new Vector4[m_NumTotalVertices];
            m_RestLengths = new float[m_NumTotalVertices];
            m_TriangleIndices = new int[m_NumTotalStrands * (m_NumVerticesPerStrand - 1) * 6];
            if(m_NumTessellationPerStrand > 0)
                m_TessellatedIndices = new int[m_NumTotalStrands * (m_NumTessellationPerStrand - 1) * 6];

            // compute tangent vectors
            ComputeStrandTangent();

            // compute rest lengths
            ComputeRestLengths();

            // triangle index
            FillTriangleIndexArray();

            for (int i = 0; i < m_NumTotalStrands; i++)
                m_StrandTypes[i] = 0;

            return true;
        }

        // AMD Ref: TressFXAsset.cpp #276
        void FillTriangleIndexArray()
        {
            Assert.IsTrue(m_NumTotalVertices == m_NumTotalStrands * m_NumVerticesPerStrand);
            Assert.IsTrue(m_TriangleIndices.Length != 0);

            int id = 0;
            int iCount = 0;

            for (int i = 0; i < m_NumTotalStrands; i++)
            {
                for (int j = 0; j < m_NumVerticesPerStrand - 1; j++)
                {
                    m_TriangleIndices[iCount++] = 2 * id;
                    m_TriangleIndices[iCount++] = 2 * id + 1;
                    m_TriangleIndices[iCount++] = 2 * id + 2;
                    m_TriangleIndices[iCount++] = 2 * id + 2;
                    m_TriangleIndices[iCount++] = 2 * id + 1;
                    m_TriangleIndices[iCount++] = 2 * id + 3;

                    id++;
                }

                id++;
            }
            Assert.IsTrue(iCount == 6 * m_NumTotalStrands * (m_NumVerticesPerStrand - 1));


            if (m_NumTessellationPerStrand > 0)
            { 
                id = 0;
                iCount = 0;

                for (int i = 0; i < m_NumTotalStrands; i++)
                {
                    for (int j = 0; j < m_NumTessellationPerStrand - 1; j++)
                    {
                        m_TessellatedIndices[iCount++] = 2 * id;
                        m_TessellatedIndices[iCount++] = 2 * id + 1;
                        m_TessellatedIndices[iCount++] = 2 * id + 2;
                        m_TessellatedIndices[iCount++] = 2 * id + 2;
                        m_TessellatedIndices[iCount++] = 2 * id + 1;
                        m_TessellatedIndices[iCount++] = 2 * id + 3;

                        id++;
                    }

                    id++;
                }
                Assert.IsTrue(iCount == 6 * m_NumTotalStrands * (m_NumTessellationPerStrand - 1));
            }
        }

        // AMD Ref: TressFXAsset.cpp #304
        public void ComputeStrandTangent()
        {            
            for (int iStrand = 0; iStrand < m_NumTotalStrands; ++iStrand)
            {
                long indexRootVertMaster = iStrand * m_NumVerticesPerStrand;

                // vertex 0
                {
                    Vector4 vert_0 = m_Positions[indexRootVertMaster];
                    Vector4 vert_1 = m_Positions[indexRootVertMaster + 1];

                    Vector4 tangent = vert_1 - vert_0;
                    tangent.Normalize();
                    try
                    {
                        m_Tangents[indexRootVertMaster] = tangent;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                // vertex 1 through n-1
                for (int i = 1; i < (int) m_NumVerticesPerStrand - 1; i++)
                {
                    Vector4 vert_i_minus_1  = m_Positions[indexRootVertMaster + i - 1];
                    Vector4 vert_i          = m_Positions[indexRootVertMaster + i];
                    Vector4 vert_i_plus_1   = m_Positions[indexRootVertMaster + i + 1];

                    Vector4 tangent_pre = vert_i - vert_i_minus_1;
                    tangent_pre.Normalize();

                    Vector4 tangent_next = vert_i_plus_1 - vert_i;
                    tangent_next.Normalize();

                    Vector4 tangent = tangent_pre + tangent_next;
                    tangent.Normalize();

                    m_Tangents[indexRootVertMaster + i] = tangent;
                }
            }           
        }

        // AMD Ref: TressFXAsset.cpp #377
        public void ComputeRestLengths()
        {
            int index = 0;

            // Calculate rest lengths
            for (int i = 0; i < m_NumTotalStrands; i++)
            {
                long indexRootVert = i * m_NumVerticesPerStrand;

                for (int j = 0; j < m_NumVerticesPerStrand - 1; j++)
                {
                    Vector4 len = (m_Positions[indexRootVert + j] - m_Positions[indexRootVert + j + 1]);
                    // Magnitude needs to calculate the vector3 xyz only, not w
                    Vector3 len_xyz = new Vector3(len.x, len.y, len.z);
                    m_RestLengths[index++] = len_xyz.magnitude;
                }

                // Since number of edges are one less than number of vertices in hair strand, below
                // line acts as a placeholder.
                m_RestLengths[index++] = 0;
            }
        }

        #endregion

        #region IO
        //protected void EI_Seek(FileStream ioObject, uint offset = 0)
        protected void EI_Seek(Stream ioObject, uint offset = 0)
        {
            //ioObject.offset = (int)offset;
            ioObject.Seek(offset, SeekOrigin.Begin);
        }

        //void EI_Read(ref TressFXTFXFileHeader header, FileStream ioObject)
        void EI_Read(ref TressFXTFXFileHeader header, Stream ioObject)
        {
            using (BinaryReader binaryReader = new BinaryReader((Stream)ioObject, System.Text.Encoding.Default, true))
            {
                header.version                  = binaryReader.ReadSingle();
                header.numHairStrands           = binaryReader.ReadUInt32();
                header.numVerticesPerStrand     = binaryReader.ReadUInt32();
                header.offsetVertexPosition     = binaryReader.ReadUInt32();
                header.offsetStrandUV           = binaryReader.ReadUInt32();
                header.offsetVertexUV           = binaryReader.ReadUInt32();
                header.offsetStrandThickness    = binaryReader.ReadUInt32();
                header.offsetVertexColor        = binaryReader.ReadUInt32();
                header.reserved                 = new uint[32];
                binaryReader.Close();
            }
        }

        //void EI_Read(ref Vector4[] vec4, long size, FileStream ioObject)
        void EI_Read(ref Vector4[] vec4, long size, Stream ioObject)
        {
            using (BinaryReader binaryReader = new BinaryReader((Stream)ioObject, System.Text.Encoding.Default, true))
            {
                int p = 0;
                for (int i = 0; i < size; i += (sizeof(float) * 4))
                {
                    // each float for Vector components (x,y,z,w) 
                    for (int j = 0; j < 4; j++)
                    {
                        vec4[p][j] = binaryReader.ReadSingle();
                    }
                    p++;
                }
                binaryReader.Close();
            }
        }

       // void EI_Read(ref Vector2[] vec2, uint size, FileStream ioObject)
        void EI_Read(ref Vector2[] vec2, uint size, Stream ioObject)
        {
            using (BinaryReader binaryReader = new BinaryReader((Stream)ioObject, System.Text.Encoding.Default, true))
            {
                int p = 0;
                for (int i = 0; i < size; i += (sizeof(float) * 2), p++)
                {
                    // each float for Vector components (x,y) 
                    for (int j = 0; j < 2; j++)
                    {
                        vec2[p][j] = binaryReader.ReadSingle();
                    }
                }
                binaryReader.Close();
            }
        }
        #endregion
    }
}