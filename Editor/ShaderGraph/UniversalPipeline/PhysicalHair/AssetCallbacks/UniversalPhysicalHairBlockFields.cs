using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class UniversalPhysicalHairBlockFields
    {
        [GenerateBlocks("Universal Render Pipeline/Physical Hair")]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor HairStrandDirection = new BlockFieldDescriptor(SurfaceDescription.name, "HairStrandDirection", "Hair Strand Direction", "SURFACEDESCRIPTION_HAIRSTRANDDIRECTION",
                new Vector3Control(new Vector3(0, -1, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor CuticleAngle = new BlockFieldDescriptor(SurfaceDescription.name, "CuticleAngle", "Cuticle Angle", "SURFACEDESCRIPTION_CUTICLEANGLE",
                new FloatControl(3.0f),ShaderStage.Fragment);
            public static BlockFieldDescriptor RadialSmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "RadialSmoothness", "Radial Smoothness", "SURFACEDESCRIPTION_RADIALSMOOTHNESS",
                new FloatControl(0.7f),ShaderStage.Fragment);

        }
    }

}
