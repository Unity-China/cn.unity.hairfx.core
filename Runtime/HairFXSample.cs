using System;
using System.Collections.Generic;

namespace HairFX
{
    // AMD Ref: TressFXSample.h #47
    [Serializable]
    public class HairFXObjectDescription
    {
        public HairFXAsset hairAsset;
        public bool customize;
        public HairFXSimulationSettings localSimulationSettings;
        public HairFXRenderingSettings localRenderingSettings;

        public bool strandsSettingToggle;
        public bool geometrySettingToggle;
        public bool simulationSettingToggle;

        public bool localStrandsFoldout;
        public bool localGeometryFoldout;
        public bool localSimulationFoldout;
    };

    //// AMD Ref: TressFXSample.h # 83
    //public struct HairFXObject
    //{
    //    public HairStrands hairStrands;
    //    public HairFXSimulationSettings hairSettings;
    //    public HairFXRenderingSettings renderingSettings;
    //    public string name;
    //};

    //// AMD Ref: TressFXSample.h #91
    //public struct TressFXScene
    //{
    //    public List<HairFXObject> objects;
    //};
}