# cn.unity.hairfx.core
Unity HairFX is a strands based hair system, this supports for Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP). This hair system is based on open source AMD-TressFX hair framework.

## Requirements

- Unity 2021.2.13f1 +
- Universal / HD Render Pipeline 12.1.4 +



## Features

This package includes a new strand base hair system that integrated from the AMD’s TressFX hair system. This hair system supports both Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP). Our goal is that this real-time strand hair simulation and rendering system support for cross platform from desktop computer, consoles to mobile devices.

While this hair system has a lot more work remains to be done, we’re sharing what we have at the moment. You’re free to use or modify it to fit your productions and build if needed.

## Note
- The TressFX Exporter plugin for Maya is in Libray/PackageCache/cn.unity.hairfx.core/**Maya_TressFX_Exporter~** folder or download [here](https://github.com/Unity-China/cn.unity.hairfx.core/tree/main/Maya_TressFX_Exporter~)

- This hair system is not the same hair system that show in ENEMIES demo video, which is separate hair system that created by Unity DemoTeam, you can find it in [github](https://github.com/Unity-Technologies/com.unity.demoteam.hair)

- This package need to include [URP](https://github.com/Unity-China/cn.unity.hairfx.urp) / [HDRP](https://github.com/Unity-China/cn.unity.hairfx.hdrp) HairFX Shader Package, see the <b> Usage </b> below

## Usage

Declare the package and its dependencies as git dependencies in `Packages/manifest.json`:

### URP:

```
"dependencies": {
    "cn.unity.hairfx.core": "https://github.com/Unity-China/cn.unity.hairfx.core.git",  
    "cn.unity.hairfx.urp": "https://github.com/Unity-China/cn.unity.hairfx.urp.git",
    ...
}
```

### HDRP:

```
"dependencies": {
    "cn.unity.hairfx.core": "https://github.com/Unity-China/cn.unity.hairfx.core.git",  
    "cn.unity.hairfx.hdrp": "https://github.com/Unity-China/cn.unity.hairfx.hdrp.git",
    ...
}
```

## Tips
(URP only) If you build this hair system to mobile platform, please try to limit the total hair strand count less then 10 thousand in the viewing screen to maintain good frame rate. The performance of FPS is also depend on your device hardware.


## Known Issues
(URP only) Currently not supports Huawei mobile phones due to some device does not support StructuredBuffer for vertex program in shader

## Related links
[AMD TressFX](https://github.com/GPUOpen-Effects/TressFX)
