/* Note:
 * Added Keyword UNIVERSAL_PIPELINE in every passes for VertexStrandsGroom.hlsl or HairStrands.hlsl
 * Added Keyword _USE_LIGHT_FACING_NORMAL (as complexLit) for Strands geometry type
 * 
 * Need _LightDirection and float3 _LightPosition variables in VertexStrandsGroom.hlsl or HairStrands.hlsl which it is vertex program, 
 * so added a custom keyword with the modified Varyings.hlsl.
 *
 * This Shader should work with 2021.3 to 2022.2 (2022.3 has not released yet)
*/

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalPhysicalHairSubTarget : UniversalSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("50970b44ae08c7b44ab245c46239b829"); // UniversalPhysicalHairSubTarget.cs

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;
        
#if UNITY_2022_2_OR_NEWER
        static PragmaCollection DOTSForward   = CorePragmas.ForwardSM45;
        static PragmaCollection DOTSInstanced = CorePragmas.InstancedSM45;
        static PragmaCollection DOTSDefault   = CorePragmas.DefaultSM45;
#else
        static PragmaCollection DOTSForward   = CorePragmas.DOTSForward;
        static PragmaCollection DOTSInstanced = CorePragmas.DOTSInstanced;
        static PragmaCollection DOTSDefault   = CorePragmas.DOTSDefault;
#endif

        public enum GeometryType
        {
            Cards,
            Strands,
        }

        public GeometryType geometryType = GeometryType.Cards;

        public UniversalPhysicalHairSubTarget() => displayName = "Physical Hair";

        protected override ShaderID shaderID => ShaderID.SG_Lit;

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }
        
        private bool complexLit // use it as Strands geometry type (useLightFacingNormal)
        {
            get
            {
                // Rules for switching to ComplexLit with forward only pass
                if (geometryType == GeometryType.Strands)
                    return true;
                else
                    return false;
            }
        }
        public override bool IsActive() => true;
        
        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
                context.AddCustomEditorForRenderPipeline(typeof(ShaderGraphLitGUI).FullName, universalRPType);

            // Process SubShaders
            context.AddSubShader(SubShaders.LitComputeDotsSubShader(target, target.renderType, target.renderQueue, complexLit));
            context.AddSubShader(SubShaders.LitGLESSubShader(target, target.renderType, target.renderQueue, complexLit));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                //material.SetFloat(Property.SpecularWorkflowMode, (float)workflowMode);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphLitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit -- always controlled by subtarget
            context.AddField(UniversalFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(UniversalFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));

        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            
            context.AddBlock(UniversalPhysicalHairBlockFields.SurfaceDescription.HairStrandDirection);
            context.AddBlock(UniversalPhysicalHairBlockFields.SurfaceDescription.CuticleAngle);
         
            context.AddBlock(BlockFields.SurfaceDescription.Alpha,              (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(UniversalPhysicalHairBlockFields.SurfaceDescription.RadialSmoothness);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if using material control, add the material property to control workflow mode
            if (target.allowMaterialOverride)
            {
                //collector.AddFloatProperty(Property.SpecularWorkflowMode, (float)workflowMode);
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);
            universalTarget.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);

            universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: true);

            context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });

            // Unity Hair
            context.AddProperty("Geometry Type", new EnumField(GeometryType.Cards) { value = geometryType }, (evt) =>
            {
                if (Equals(geometryType, evt.newValue))
                    return;

                registerUndo("Change Geometry Type");
                geometryType = (GeometryType)evt.newValue;
                onChange();
            });
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
            return hash;
        }

        #region SubShader
        static class SubShaders
        {
            // SM 4.5, compute with dots instancing
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool complexLit)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                // Always render in Forward path (No Gbuffer pass)
                result.passes.Add(LitPasses.Forward(target, complexLit, DOTSForward));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(LitPasses.ShadowCaster(target), DOTSInstanced)); // custom

                if (target.mayWriteDepth)
                    result.passes.Add(PassVariant(LitPasses.DepthOnly(target), DOTSInstanced));

                if (complexLit)
                    result.passes.Add(PassVariant(LitPasses.DepthNormalOnly(target), DOTSInstanced));
                else
                    result.passes.Add(PassVariant(LitPasses.DepthNormal(target), DOTSInstanced));
                result.passes.Add(PassVariant(LitPasses.Meta(target), DOTSDefault));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(LitPasses.SceneSelection(target), DOTSDefault));
                result.passes.Add(PassVariant(LitPasses.ScenePicking(target), DOTSDefault));
                
                result.passes.Add(PassVariant(LitPasses._2D(target), DOTSDefault));
                
                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(UniversalTarget target,  string renderType, string renderQueue, bool complexLit)
            {
                // SM 2.0, GLES

                // ForwardOnly pass is used as complex Lit SM 2.0 fallback for GLES.
                // Drops advanced features and renders materials as Lit.

                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };
                
                result.passes.Add(LitPasses.Forward(target, complexLit));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(LitPasses.ShadowCaster(target)); // custom

                if (target.mayWriteDepth)
                    result.passes.Add(LitPasses.DepthOnly(target));

                if (complexLit)
                    result.passes.Add(LitPasses.DepthNormalOnly(target));
                else
                    result.passes.Add(LitPasses.DepthNormal(target));
                result.passes.Add(LitPasses.Meta(target));
                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(LitPasses.SceneSelection(target));
                result.passes.Add(LitPasses.ScenePicking(target));

                result.passes.Add(LitPasses._2D(target));
                return result;
            }
        }
        #endregion

        #region Pass
        static class LitPasses
        {
            static void AddUseLightFacingNormalControlToPass(ref PassDescriptor pass, UniversalTarget target, bool complexLit)
            {
                //if (target.allowMaterialOverride)
                //    pass.keywords.Add(LitKeywords.UseLightFacingNormal);
                //else 
                if (complexLit)
                    pass.defines.Add(LitKeywords.UseLightFacingNormal, 1);
            }
            
            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, UniversalTarget target, bool receiveShadows)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(LitKeywords.ReceiveShadowsOff);
                else if (!receiveShadows)
                    pass.defines.Add(LitKeywords.ReceiveShadowsOff, 1);
            }

            // Override as Forward only
            public static PassDescriptor Forward(UniversalTarget target, bool complexLit, PragmaCollection pragmas = null )
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    //lightMode = "UniversalForward",
                    lightMode =  "UniversalForwardOnly", // Using Forward for both Forward and Deferred path
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = pragmas ?? CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, LitDefines.DefineUniversalPipline}, // custom
                    keywords = new KeywordCollection() { LitKeywords.Forward },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                AddUseLightFacingNormalControlToPass(ref result, target, complexLit); // custom

                return result;
            }
/*
            public static PassDescriptor ForwardOnly(
                UniversalTarget target,
                bool complexLit,
                BlockFieldDescriptor[] vertexBlocks,
                BlockFieldDescriptor[] pixelBlocks,
                PragmaCollection pragmas)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = vertexBlocks,
                    validPixelBlocks = pixelBlocks,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = pragmas,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection() { LitKeywords.Forward },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                if (complexLit)
                    result.defines.Add(LitKeywords.UseLightFacingNormal, 1);            // custom

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                //AddWorkflowModeControlToPass(ref result, target, workflowMode);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                AddUseLightFacingNormalControlToPass(ref result, target, complexLit);   // custom

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.DOTSGBuffer,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection() { LitKeywords.GBuffer },
                    includes = LitIncludes.GBuffer,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                //AddWorkflowModeControlToPass(ref result, target, workflowMode);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                //AddUseLightFacingNormalControlToPass(ref result, target, false); // need it?

                return result;
            }
*/
            public static PassDescriptor Meta(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = CorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = LitIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor _2D(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    referenceName = "SHADERPASS_2D",
                    lightMode = "Universal2D",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(){ LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection(),
                    includes = LitIncludes._2D,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
            
            public static PassDescriptor DepthNormal(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormals",
                    referenceName = "SHADERPASS_DEPTHNORMALS",
                    lightMode = "DepthNormals",
                    useInPreview = false,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(){ LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection(),
                    includes = CoreIncludes.DepthNormalsOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormalOnly(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormalsOnly",
                    referenceName = "SHADERPASS_DEPTHNORMALSONLY",
                    lightMode = "DepthNormalsOnly",
                    useInPreview = false,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(){ LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection(),
                    includes = CoreIncludes.DepthNormalsOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            //=============================================================================
            // Requires DefineUniversalPipline
            public static PassDescriptor ShadowCaster(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "ShadowCaster",
                    referenceName = "SHADERPASS_SHADOWCASTER",
                    lightMode = "ShadowCaster",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.ShadowCaster,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ShadowCaster(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(){ LitDefines.DefineUniversalPipline },  // custom
                    keywords = new KeywordCollection() { CoreKeywords.ShadowCaster },
                    includes = LitIncludes.ShadowCaster,                                    // custom

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
            
            public static PassDescriptor DepthOnly(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthOnly",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "DepthOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection() { LitDefines.DefineUniversalPipline }, // custom
                    keywords = new KeywordCollection(),
                    includes = CoreIncludes.DepthOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor SceneSelection(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "SceneSelectionPass",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "SceneSelectionPass",
                    useInPreview = false,

                    // Template
                    passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.SceneSelection(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection { CoreDefines.SceneSelection, LitDefines.DefineUniversalPipline , { CoreKeywordDescriptors.AlphaClipThreshold, 1 }}, // custom
                    keywords = new KeywordCollection(),
                    includes = CoreIncludes.SceneSelection,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor ScenePicking(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "ScenePickingPass",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "Picking",
                    useInPreview = false,

                    // Template
                    passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ScenePicking(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection { CoreDefines.ScenePicking, LitDefines.DefineUniversalPipline, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } }, // custom
                    keywords = new KeywordCollection(),
                    includes = CoreIncludes.ScenePicking,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
            //==================================================================================
        }
        #endregion

        #region PortMasks
        static class LitBlockMasks
        {

            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,

                UniversalPhysicalHairBlockFields.SurfaceDescription.HairStrandDirection,
                UniversalPhysicalHairBlockFields.SurfaceDescription.CuticleAngle,
                UniversalPhysicalHairBlockFields.SurfaceDescription.RadialSmoothness,
            };

            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class LitRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                #if !UNITY_2022_1_OR_NEWER
                StructFields.Varyings.viewDirectionWS,
                #endif
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };
/*
            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };
*/
            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.uv0,                            //
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            // needed for meta UVs
                StructFields.Attributes.instanceID,                     // needed for rendering instanced terrain
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,                        // needed for meta UVs
                StructFields.Varyings.texCoord1,                        // VizUV
                StructFields.Varyings.texCoord2,                        // LightCoord
            };
        }
        #endregion

        #region Defines
        static class LitDefines
        {

            public static readonly DefineCollection DefineUniversalPipline = new DefineCollection()
            {
                { LitKeywords.DefinePipeline, 1},
            };
        }
        #endregion

        #region Keywords
        static class LitKeywords
        {
            //==============================================================================================
            // Additional Unity Hair Keywords
            //==============================================================================================
            // Always define this keyword for this Hair shader when included the VertexStrandsGroom.hlsl or HairStrands.hlsl
            // This keyword will not affect anything, If user is using this shader for mesh based type of hair, it should working as normally.
            public static readonly KeywordDescriptor DefinePipeline = new KeywordDescriptor()
            {
                displayName = "Universal Pipeline",
                referenceName = "UNIVERSAL_PIPELINE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor UseLightFacingNormal = new KeywordDescriptor()
            {
                displayName = "Use Light Facing Normal",
                referenceName = "_USE_LIGHT_FACING_NORMAL", // use #if instead of #ifdef
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            //==============================================================================================

            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor ScreenSpaceAmbientOcclusion = new KeywordDescriptor()
            {
                displayName = "Screen Space Ambient Occlusion",
                referenceName = "_SCREEN_SPACE_OCCLUSION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static readonly KeywordDescriptor ForwardPlus = new KeywordDescriptor()
            {
                displayName = "Forward Plus",
                referenceName = "_FORWARD_PLUS",
                type = KeywordType.Boolean,
                stages = KeywordShaderStage.All,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
            
            public static readonly KeywordDescriptor LODFadeCrossFade = new KeywordDescriptor()
            {
                displayName = "LOD Fade Cross Fade",
                referenceName = "LOD_FADE_CROSSFADE",
                type = KeywordType.Boolean,
                stages = KeywordShaderStage.Fragment,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { ScreenSpaceAmbientOcclusion },
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.LightCookies },
                #if !UNITY_2022_2_OR_NEWER
                { CoreKeywordDescriptors.ClusteredRendering },
                #endif
                #if UNITY_2022_2_OR_NEWER
                {ForwardPlus},
                {LODFadeCrossFade},
                #endif
            };
/*
            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.RenderPassEnabled },
                { CoreKeywordDescriptors.DebugDisplay },
            };
*/            
        }
        #endregion

        #region Includes
        static class LitIncludes
        {
            const string kShadows               = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput             = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            //const string kForwardPass         = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            //const string kGBuffer               = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";
            //const string kPBRGBufferPass        = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl";
            const string kLightingMetaPass      = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string k2DPass                = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl";

            const string kShaderPass            = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
            const string kShadowCasterPass      = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
            const string kDepthOnlyPass         = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
            const string kDepthNormalsOnlyPass  = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl";

            //==================================================================================================
            // Custom Hair path :                  Packages/com.unity.hairfx/Editor/ShaderGraph/UniversalPipeline/PhysicalHair/Includes/
            const string kForwardPass           = "Packages/com.unity.hairfx/Editor/ShaderGraph/UniversalPipeline/PhysicalHair/Includes/MarschnerHairForwardPass.hlsl";
            const string kHairSurfaceData       = "Packages/com.unity.hairfx/Editor/ShaderGraph/UniversalPipeline/PhysicalHair/Includes/MarschnerHairSurfaceData.hlsl";
            const string kHairBSDF              = "Packages/com.unity.hairfx/Editor/ShaderGraph/UniversalPipeline/PhysicalHair/Includes/MarschnerHairBSDF.hlsl";

            // Custom Varying.hlsl with custom define _LightDirection, that affect the pass of Forward and ShadowCaster only
            const string kVaryings              = "Packages/com.unity.hairfx/Editor/ShaderGraph/UniversalPipeline/PhysicalHair/Includes/Varyings.hlsl";


            //==================================================================================================
            // Override to get the custom Varyings.hlsl path
            //==================================================================================================
            public static readonly IncludeCollection CorePostgraph = new IncludeCollection
            {
                { kShaderPass, IncludeLocation.Pregraph },
                { kVaryings, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthOnly = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CorePostgraph },
                { kDepthOnlyPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CorePostgraph },
                { kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection ShadowCaster = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CorePostgraph },
                { kShadowCasterPass, IncludeLocation.Postgraph },
            };
            //==================================================================================================

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CoreIncludes.CorePostgraph },
                { kHairSurfaceData, IncludeLocation.Postgraph },
                { kHairBSDF, IncludeLocation.Postgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };
/*
            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CorePostgraph },
                
                { kHairSurfaceData, IncludeLocation.Postgraph },
                { kHairBSDF, IncludeLocation.Postgraph },
                { kGBuffer, IncludeLocation.Postgraph },
                { kPBRGBufferPass, IncludeLocation.Postgraph },
            };
*/
            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kMetaInput, IncludeLocation.Pregraph },

                // Post-graph
                //{ CorePostgraph },
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection _2D = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                //{ CorePostgraph },
                { CorePostgraph },
                { k2DPass, IncludeLocation.Postgraph },
            };
        }

        
        #endregion

    }
}
