using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace HairFX
{
    internal class Utilities
    {
        private static readonly Random rand = new Random();


        //--------------------------------------------------------------------------------------
        //
        // GetTangentVectors
        //
        // Create two arbitrary tangent vectors (t0 and t1) perpendicular to the input normal vector (n).
        //
        //--------------------------------------------------------------------------------------
        public static void GetTangentVectors(Vector3 n, out Vector3 t0, out Vector3 t1)
        {
            t0 = new Vector3();
            t1 = new Vector3();

            if (Mathf.Abs(n.z) > 0.707f)
            {
                var a = n.y * n.y + n.z * n.z;
                var k = 1.0f / Mathf.Sqrt(a);
                t0.x = 0;
                t0.y = -n.z * k;
                t0.z = n.y * k;

                t1.x = a * k;
                t1.y = -n.x * t0.z;
                t1.z = n.x * t0.y;
            }
            else
            {
                var a = n.x * n.x + n.y * n.y;
                var k = 1.0f / Mathf.Sqrt(a);
                t0.x = -n.y * k;
                t0.y = n.x * k;
                t0.z = 0;

                t1.x = -n.z * t0.y;
                t1.y = n.z * t0.x;
                t1.z = a * k;
            }
        }

        public static float GetRandom(float Min, float Max)
        {
            return (float) rand.NextDouble() * (Max - Min) + Min;
        }

        public static ComputeBuffer InitializeBuffer(Array data, int stride)
        {
            var returnBuffer = new ComputeBuffer(data.Length, stride);
            returnBuffer.SetData(data);
            return returnBuffer;
        }

        // RGBAFloat format Render Texture with size of width and height.
        public static RenderTexture GenerateRenderTextureRGBA(Vector4[] data, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

            RenderTexture renderTexture = new RenderTexture(width, height, 1, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            Graphics.Blit(texture, renderTexture);

            return renderTexture;
        }

        // RFloat format Texture2D with size of width and height.
        public static Texture2D GenerateTexture2DR(float[] data, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RFloat, false);
            Color color = new Color();
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    color.r = data[i * height + j];
                    texture.SetPixel(i, j, color);
                }
            texture.Apply();
            return texture;
        }

        // RGFloat format Texture2D with size of width and height.
        public static Texture2D GenerateTexture2DRG(Vector2[] data, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGFloat, false);
            Color color = new Color();
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    color.r = data[i * height + j].x;
                    color.g = data[i * height + j].y;
                    texture.SetPixel(i, j, color);
                }
            texture.Apply();
            return texture;
        }
    }
}