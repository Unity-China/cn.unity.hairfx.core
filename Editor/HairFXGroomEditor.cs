using UnityEditor;
using UnityEngine;

namespace HairFX
{
    [CustomEditor(typeof(HairFXGroom))]
    [CanEditMultipleObjects]
    public class HairFXGroomEditor : Editor
    {
        Editor              m_ProfileEditor;
        HairFXGroom         m_HairFXGroom;
        SerializedObject    m_SerializedObject;

        GUIContent textureBufferGUIContent  = new GUIContent("Use Texture Buffer", "Enable this option for some mobile GPU that don't support Structured Buffer in vertex program. (ARM: Mali GPU)");

        private void OnEnable()
        {
            m_HairFXGroom = (HairFXGroom)target;
            m_SerializedObject = new SerializedObject(m_HairFXGroom);
        }

        public override void OnInspectorGUI()
        {
            m_SerializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawPropertiesExcluding(m_SerializedObject, "m_Script", "useRenderTexture", "needToReload", "DebugBoundingBox", "HairBounds");

            // reload unity hair shape if hair profile changed
            if (EditorGUI.EndChangeCheck()) m_HairFXGroom.needToReload = true;

            /// no profile, do nothing
            if (m_HairFXGroom.HairProfile == null)
            {
                EditorGUILayout.HelpBox("Please assign an Unity Hair Profile here.", MessageType.Warning);
                if (m_ProfileEditor != null) DestroyImmediate(m_ProfileEditor);
            }
            else
            {
                if (m_ProfileEditor == null)
                {
                    m_ProfileEditor = CreateEditor(m_HairFXGroom.HairProfile);
                }

                if (m_ProfileEditor.target != m_HairFXGroom.HairProfile)
                {
                    /// Profile has changed? then create the new editor.
                    if (m_ProfileEditor != null) DestroyImmediate(m_ProfileEditor);
                    m_ProfileEditor = CreateEditor(m_HairFXGroom.HairProfile);
                    m_HairFXGroom.needToReload = true;
                }

                m_ProfileEditor.OnInspectorGUI();

                // reload unity hair shape if hair profile editor says hair profile need reload
                if (((HairFXProfileEditor)m_ProfileEditor).hairProfileNeedReload == true)
                {
                    m_HairFXGroom.needToReload = true;
                    ((HairFXProfileEditor)m_ProfileEditor).hairProfileNeedReload = false;
                }


                //EditorGUI.BeginDisabledGroup(Application.isPlaying);
                //m_HairFXGroom.useRenderTexture = EditorGUILayout.Toggle(textureBufferGUIContent, m_HairFXGroom.useRenderTexture);
                //EditorGUI.EndDisabledGroup();
            }

            // init hair objects at first time
            if (m_HairFXGroom.hairObjects == null) m_HairFXGroom.needToReload = true;

            if (m_HairFXGroom.needToReload == true)
            {
               m_HairFXGroom.ReloadHairObjects();
               m_HairFXGroom.needToReload = false;
            }

            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
