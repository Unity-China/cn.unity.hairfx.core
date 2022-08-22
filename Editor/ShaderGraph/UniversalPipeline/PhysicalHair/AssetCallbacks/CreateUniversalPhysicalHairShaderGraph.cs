using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateUniversalPhysicalHairShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Physical Hair Shader Graph", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateUniversalHairGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalPhysicalHairSubTarget));

            var blockDescriptors = new [] 
            { 
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,                
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                
                UniversalPhysicalHairBlockFields.SurfaceDescription.HairStrandDirection,
                UniversalPhysicalHairBlockFields.SurfaceDescription.CuticleAngle,
                UniversalPhysicalHairBlockFields.SurfaceDescription.RadialSmoothness,
            };

            GraphUtil.CreateNewGraphWithOutputs(new [] {target}, blockDescriptors);
        }
    }
}
