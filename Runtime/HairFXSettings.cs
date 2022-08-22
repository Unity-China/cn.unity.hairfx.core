// ----------------------------------------------------------------------------
// Wrappers for setting values that end up in constant buffers.
// ----------------------------------------------------------------------------
using System;
using UnityEngine;

namespace HairFX
{
    // AMD Ref: TressFXSetting.h #35
    // Probably want to unify or rename these.
    [Serializable]
    public class HairFXSimulationSettings
    {
        // VSPf
        public FloatHairSetting vspCoeff                    = new FloatHairSetting{ value = 0.3f };
        public FloatHairSetting vspAccelThreshold           = new FloatHairSetting { value = 0.7f };

        // damping
        public FloatHairSetting damping                     = new FloatHairSetting { value = 0.1f };

        // local constraint
        // public FloatHairSetting localConstraintStiffness    = new FloatHairSetting { value = 0.5f };
        public IntHairSetting localConstraintsIterations    = new IntHairSetting { value = 10 };
        public CurveHairSetting localStiffnessCurve         = new CurveHairSetting { value = new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 0.4f)) };

        // global constraint
        // public FloatHairSetting globalConstraintStiffness   = new FloatHairSetting { value = 0.8f };
        // public FloatHairSetting globalConstraintsRange      = new FloatHairSetting { value = 0.4f };
        public CurveHairSetting globalStiffnessCurve        = new CurveHairSetting { value = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0.05f)) };

        // length constraint
        public IntHairSetting lengthConstraintsIterations   = new IntHairSetting { value = 2 };

        // gravity
        public FloatHairSetting gravityMagnitude            = new FloatHairSetting { value = 1.0f };

        // tip separation for follow hair from its guide
        public FloatHairSetting tipSeparation               = new FloatHairSetting { value = 0f };

        // length offset
        public FloatHairSetting followHairLengthOffset      = new FloatHairSetting { value = 0.3f };

        // clamp position delta
        // public FloatHairSetting clampPositionDelta          = new FloatHairSetting { value = 2f };
    };

    // AMD Ref: TressFXSetting.h #91
    [Serializable]
    public class HairFXRenderingSettings
    {
        // LOD settings
        public FloatRangeHairSetting LODRange               = new FloatRangeHairSetting { value = new Vector2(1.0f, 8f) };
        public FloatHairSetting LODPercent                  = new FloatHairSetting { value = 0.5f };
        public FloatHairSetting LODWidthMultiplier          = new FloatHairSetting { value = 2.0f };

        // General information
        public FloatHairSetting fiberRadius                 = new FloatHairSetting { value = 0.2f };
        public FloatHairSetting fiberRatio                  = new FloatHairSetting { value = 0.1f };
        public FloatHairSetting fiberRatioStart             = new FloatHairSetting { value = 0.2f };
        public FloatHairSetting tipPercentage               = new FloatHairSetting { value = 0.0f };
        public FloatHairSetting strandUVTilingFactor        = new FloatHairSetting { value = 1.0f };

        // For deep approximated shadow lookup
        public FloatHairSetting shadowLODStartDistance      = new FloatHairSetting { value = 1.0f };
        public FloatHairSetting shadowLODEndDistance        = new FloatHairSetting { value = 5.0f };
        public FloatHairSetting shadowLODPercent            = new FloatHairSetting { value = 0.5f };
        public FloatHairSetting shadowLODWidthMultiplier    = new FloatHairSetting { value = 2.0f };

        // enable ThinTip and LOD
        public BoolHairSetting enableThinTip                = new BoolHairSetting { value = true };
        public BoolHairSetting enableHairLOD                = new BoolHairSetting { value = true };

        // hair
        public FloatHairSetting followHairOffset            = new FloatHairSetting { value = 0.015f };
        public IntHairSetting followHairCount               = new IntHairSetting { value = 4 };
        public BoolHairSetting bothEndsImmovable            = new BoolHairSetting { value = false };

        // tessellation
        public EnumHairSetting tessellationNumber           = new EnumHairSetting { value = TessellationNumber._32 };
    };

    [Serializable]
    public struct FloatHairSetting
    {
        public float value;
        public bool overrideStates;
    }

    [Serializable]
    public struct FloatRangeHairSetting
    {
        public Vector2 value;
        public bool overrideStates;
    }

    [Serializable]
    public struct IntHairSetting
    {
        public int value;
        public bool overrideStates;
    }

    [Serializable]
    public struct BoolHairSetting
    {
        public bool value;
        public bool overrideStates;
    }

    public enum TessellationNumber { _off = 0, _8 = 8, _16 = 16, _32 = 32, _64 = 64, _128 = 128 };
    [Serializable]
    public struct EnumHairSetting
    {
        public TessellationNumber value;
        public bool overrideStates;
    }

    [Serializable]
    public struct CurveHairSetting
    {
        public AnimationCurve value;
        public bool overrideStates;
    }
}