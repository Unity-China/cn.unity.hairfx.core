# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.9.0] - 2022-06-18
* Added new Hair Demo and removed old demos
* Renamed package name to HairFX
* Renamed class name UnityHairShape to HairFXGroom
* Removed Kayjiya hair shader from URP
* 
## [0.8.1] - 2022-06-14
* Added Renderer LightLayer supports for URP

## [0.8.0] - 2022-06-10
* Remove GBuffer pass, the hair will be render as Forward in deferred path for URP
* Physical Hair Shader is compatible with 2022.2 for URP

## [0.7.0] - 2022-05-26
* New Physical hair shader with Marschner lighting model for URP
* Added Rendering debugger feature in editor for URP

## [0.6.0] - 2022-03-27
* Added importer ConvertUnit boolean option
* Auto calculate BoundingBox, fix simulation editor

## [0.5.0] - 2021-08-13
* Updated the package to 2021.2
* Removed AA expand pixel from URP

## [0.4.3] - 2021-06-03
* Editor remember hair setting foldouts open/fold
* Fix Android build bug
* Remove AA expand pixel from HDRP

## [0.4.2] - 2021-05-06
* Add AA expand pixel
* Add LOD feature and LOD debug canvas
* Add UV2 for strand color mapping

## [0.4.1] - 2021-04-19
* Fix normal and tangent flip bug
* Fix tessellation buffer out of boundary error on mobile device
* Add UV support in strand space
* Fix SSAO bug when hair transform scale changes (URP)
* Fix alpha in hair shader (URP)
* Remove VertexColor node in shader graph (URP)

## [0.4.0] - 2021-03-26
* Remove UnityHairRenderer component
* Add mesh build process to UnityHairShape
* Add MeshRenderer and MeshFilter Requirement
* Fix shader transform bug due to MeshRenderer

## [0.3.1] - 2021-03-19
* Fix LockHairTip override bug
* Hide UseTextureBuffer option
* Update hair profile default settings
* Fix tessellation buffer length bug

## [0.3.0] - 2021-01-18
* New dedicated shader-graph hair material for univerial render pipeline
* Added Tessellation feature for the hair strand
* New Curve based control for the both Global and Local Stiffness
* New Hair Demo Scene

## [0.2.0] - 2020-11-06
* Added HDRP support
* Added Melanin feature for hair color
* Removed code based shaders and now all hair shaders are created via ShaderGraph
* Draw hair directly in both Scene and Game window, it does not requires to enter the PLAY mode
* New URP hair lighting that converted from HDRP version hair
* Improved wind effect and support Unity Wind Zone
* Added Partent Transform field in UnityHairSimulation that does not requires hair gameobject have to be the child of head joint anymore
* Added Scale option setting feature when imported the tfx file in Unity Editor
* Improved overall the UnityHair Editor GUI layout


## [0.1.0] - 2020-09-12
### This is the first preview of *Unity Hair*.