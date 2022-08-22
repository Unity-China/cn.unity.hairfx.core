using UnityEngine;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace HairFX
{
    [ScriptedImporter(1, new[] { "tfx", "tfxbone" })]
    public class TFXFileImporter : ScriptedImporter
    {
        public float ScaleFactor = 1;
        [Tooltip("1cm (File) to 0.01m (Unity)")]
        public bool ConvertUnits = true;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<HairFXAsset>();
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("HairFXAsset icon")[0]));
            ctx.AddObjectToAsset("main", root, icon);
            ctx.SetMainObject(root);
            root.LoadHeaderData(ctx.assetPath, ScaleFactor * (ConvertUnits?0.01f:1f));
        }
    }
}