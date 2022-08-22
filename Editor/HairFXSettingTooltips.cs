using System.Collections.Generic;
using UnityEngine;

namespace HairFX
{
    static public class HairFXSettingTooltips
    {
        static private Dictionary<string, GUIContent> m_hairGUIContens = new Dictionary<string, GUIContent>()
        {
        { "vspCoeff",                       new GUIContent("Root Rigidness", "The strength of Velocity Shock Propagation.") },
        { "vspAccelThreshold",              new GUIContent("Acceleration Constraints", "The threshold of Velocity Shock Propagation.") },
        { "damping",                        new GUIContent("Damping", "The strength of hair simulation damping.") },
        { "localConstraintStiffness",       new GUIContent("Shape Stiffness", "The simulation stiffness of local shape.") },
        { "localConstraintsIterations",     new GUIContent("Shape Simulation Constraints", "The iteration number of local shape simulation.") },
        { "globalConstraintStiffness",      new GUIContent("Location Stiffness", "The simulation stiffness of global shape.") },
        { "globalConstraintsRange",         new GUIContent("Location Simulation Constraints", "The length percentage of global shape simulation.") },
        { "lengthConstraintsIterations",    new GUIContent("Length Constraints", "The iteration number of length constraint simulation.") },
        { "gravityMagnitude",               new GUIContent("Gravity Magnitude", "The gravity strength of hair.") },
        { "tipSeparation",                  new GUIContent("Tip Separation", "The separation strength of hair tips.") },
        { "followHairLengthOffset",         new GUIContent("Length Offset", "The length offset of follow hairs.") },
        { "followHairOffset",               new GUIContent("Hair Offset", "The position offset of generated hair from guide hair.") },
        { "followHairCount",                new GUIContent("Hair Count", "The number of generated hair from every single guide hair.") },
        { "tessellationNumber",             new GUIContent("Tessellation Per Strand", "The tessellation number of each hair strand.") },

        { "bothEndsImmovable",              new GUIContent("Lock Hair Tip", "Make the tip of hair immovable.") },
        { "enableHairLOD",                  new GUIContent("Enable LOD", "Enable hair LOD.") },
        { "LODRange",                       new GUIContent("LOD Distance", "The start and end distance of LOD.")  },
        { "LODPercent",                     new GUIContent("LOD Strand Reduction", "Remaining percentage [0 to 1] of hair at the end distance of LOD.") },
        { "LODWidthMultiplier",             new GUIContent("LOD Hair Width Multiplier", "The hair width multiplier along LOD distance.") },
        { "fiberRadius",                    new GUIContent("Hair Thickness", "The width of each hair strand.") },
        { "fiberRatio",                     new GUIContent("Tip Thinning Ratio", "The width of hair tip.") },
        { "fiberRatioStart",                new GUIContent("Tip Length", "The start percentage [0 to 1] for hair width to become thiner.") },
        { "enableThinTip",                  new GUIContent("Enable Thin Tip", "Enable thiner hair tip.") },
        { "globalStiffnessCurve",           new GUIContent("Location Stiffness", "Each hair strand's root to tip global stiffness.")},
        { "localStiffnessCurve",            new GUIContent("Shape Stiffness", "Each hair strand's root to tip local stiffness.")},
        };

        static public GUIContent Get(string name)
        {
            return m_hairGUIContens.ContainsKey(name) ? m_hairGUIContens[name] : null;
        }
    }

    // Custom for Hair Settings Editor
    public static class HairSettingGroupNames
    {
        public static string[] strandsSettingNames = {
            "followHairCount",
            "followHairOffset",
            "tessellationNumber",
            "bothEndsImmovable",
        };
        public static string[] geometrySettingNames = {
            "enableHairLOD",
            "LODRange",
            "LODPercent",
            "LODWidthMultiplier",
            "enableThinTip", 
            "fiberRadius", 
            "fiberRatio", 
            "fiberRatioStart",
        };
        public static string[] shadingSettingNames = {
            "hairMatBaseColor", 
            "hairMatTipColor", 
            "tipPercentage", 
            "hairKDiffuse",
            "hairKSpec1", 
            "hairSpecExp1", 
            "hairKSpec2", 
            "hairSpecExp2"
        };
        public static string[] simulationSettingNames =
        {
            "globalStiffnessCurve",
            "localStiffnessCurve",
            "vspCoeff",
            "vspAccelThreshold",
			"lengthConstraintsIterations",
            "damping",
            "gravityMagnitude",
            "tipSeparation",
            "followHairLengthOffset",
        };
    }

    static public class HairSettingRanges
    {
        static private Dictionary<string, Vector2> m_settingRanges = new Dictionary<string, Vector2>()
        {
        { "vspCoeff",                           new Vector2(0f, 1f) },
        { "vspAccelThreshold",                  new Vector2(0f, 1f) },
        { "damping",                            new Vector2(0f, 1f) },
        { "localConstraintStiffness",           new Vector2(0f, 1f) },
        { "localConstraintsIterations",         new Vector2(1, 20) },
        { "globalConstraintStiffness",          new Vector2(0f, 1f) },
        { "globalConstraintsRange",             new Vector2(0f, 1f) },
        { "lengthConstraintsIterations",        new Vector2(1, 20) },
        { "gravityMagnitude",                   new Vector2(0f, 10f) },
        { "tipSeparation",                      new Vector2(-1f, 2f) },
        { "followHairLengthOffset",             new Vector2(0, 1) },
        { "followHairOffset",                   new Vector2(0f, 0.1f) },
        { "followHairCount",                    new Vector2(0, 32) },
        
            
        { "LODRange",                           new Vector2(0f, 25f) },
        { "LODPercent",                         new Vector2(0f, 1f) },
        { "LODWidthMultiplier",                 new Vector2(0f, 20f) },
        { "fiberRadius",                        new Vector2(0.01f, 3f) },
        { "fiberRatio",                         new Vector2(0f, 1f) },
        { "fiberRatioStart",                    new Vector2(0f, 1f) },
        { "tipPercentage",                      new Vector2(0f, 1f) },
        { "strandUVTilingFactor",               new Vector2(0f, 1f) },
        };

        static public Vector2 Get(string name)
        {
            return m_settingRanges.ContainsKey(name) ? m_settingRanges[name] : new Vector2(0, 0);
        }
    }
}