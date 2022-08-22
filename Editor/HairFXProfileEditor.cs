using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Data.Odbc;

namespace HairFX
{
    [CustomEditor(typeof(HairFXProfile))]
    [CanEditMultipleObjects]
    public class HairFXProfileEditor : Editor
    {
        // Hairs
        HairFXProfile m_TFXHairProfile;
        SerializedObject m_SerializedObject;
        SerializedProperty m_HairListProperty;
        SerializedProperty m_GlobalSimulationSettingsProperty; //global default parameters
        SerializedProperty m_GlobalRenderingSettingsProperty;
        ReorderableList m_HairReorderableList;

        SerializedProperty hairStatusFoldout;
        SerializedProperty strandsSettingFoldout;
        SerializedProperty geometrySettingFoldout;
        SerializedProperty simulationSettingFoldout;

        SerializedProperty[] globalStrandsSerializedProperties;
        SerializedProperty[] globalGeometrySerializedProperties;
        SerializedProperty[] globalSimulationSerializedProperties;

        Vector2 propertyValueRange;
        Vector2 MinMax;
        Rect propertyRect;
        GUIContent propertyGUIContent;

        public bool hairProfileNeedReload;

        void OnEnable()
        {
            if (target == null) return;
            m_TFXHairProfile = (HairFXProfile)target;
            m_SerializedObject = new SerializedObject(m_TFXHairProfile);

            m_GlobalSimulationSettingsProperty = m_SerializedObject.FindProperty("globalSimulationSettings");
            m_GlobalRenderingSettingsProperty = m_SerializedObject.FindProperty("globalRenderingSettings");

            globalStrandsSerializedProperties = GetSerializedPropertyGroup(HairSettingGroupNames.strandsSettingNames, m_GlobalRenderingSettingsProperty);
            globalGeometrySerializedProperties = GetSerializedPropertyGroup(HairSettingGroupNames.geometrySettingNames, m_GlobalRenderingSettingsProperty);
            globalSimulationSerializedProperties = GetSerializedPropertyGroup(HairSettingGroupNames.simulationSettingNames, m_GlobalSimulationSettingsProperty);

            m_HairListProperty = m_SerializedObject.FindProperty("hairList");
            m_HairReorderableList = new ReorderableList(serializedObject, m_HairListProperty, true, true, true, true);
            m_HairReorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 5;

            hairStatusFoldout           = m_SerializedObject.FindProperty("hairStatusFoldout");
            strandsSettingFoldout       = m_SerializedObject.FindProperty("strandsSettingFoldout");
            geometrySettingFoldout      = m_SerializedObject.FindProperty("geometrySettingFoldout");
            simulationSettingFoldout    = m_SerializedObject.FindProperty("simulationSettingFoldout");

            // Header
            m_HairReorderableList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Hair Asset Group"); };

            // Element
            m_HairReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Get hair variables
                SerializedProperty element                  = m_HairListProperty.GetArrayElementAtIndex(index);
                SerializedProperty hair                     = element.FindPropertyRelative("hairAsset");
                SerializedProperty localSimulationSettings  = element.FindPropertyRelative("localSimulationSettings");
                SerializedProperty localRenderingSettings   = element.FindPropertyRelative("localRenderingSettings");
                SerializedProperty localStrandsFoldout      = element.FindPropertyRelative("localStrandsFoldout");
                SerializedProperty localGeometryFoldout     = element.FindPropertyRelative("localGeometryFoldout");
                SerializedProperty localSimulationFoldout   = element.FindPropertyRelative("localSimulationFoldout");

                float heightNow = rect.y + 4;
                EditorGUI.DrawRect(new Rect(rect.x - 20, heightNow - 2, rect.width + 26, EditorGUIUtility.singleLineHeight + 4), new Color(0f, 0f, 0f, 0.2f));

                EditorGUI.BeginChangeCheck();
                hair.objectReferenceValue = EditorGUI.ObjectField(new Rect(rect.x + 5, heightNow, rect.width, EditorGUIUtility.singleLineHeight), (hair.objectReferenceValue == null) ? "Hair File" : hair.objectReferenceValue.name, hair.objectReferenceValue, typeof(HairFXAsset), false);
                if (EditorGUI.EndChangeCheck()) hairProfileNeedReload = true;

                heightNow += EditorGUIUtility.singleLineHeight + 6;

                {
                    EditorGUI.BeginDisabledGroup(Application.isPlaying);
                    SerializedProperty[] localStrandsProperties = GetSerializedPropertyGroup(HairSettingGroupNames.strandsSettingNames, localRenderingSettings);
                    SerializedProperty strandsSettingToggle = element.FindPropertyRelative("strandsSettingToggle");

                    // auto reload when local strands setting override option change
                    EditorGUI.BeginChangeCheck();
                    strandsSettingToggle.boolValue = EditorGUI.Toggle(new Rect(rect.x - 10, heightNow, 10, EditorGUIUtility.singleLineHeight), strandsSettingToggle.boolValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        hairProfileNeedReload = true;
                    }

                    localStrandsFoldout.boolValue = EditorGUI.Foldout(new Rect(rect.x + 20, heightNow, rect.width, EditorGUIUtility.singleLineHeight), localStrandsFoldout.boolValue, "Strands Settings", true);

                    using (new EditorGUI.DisabledScope(!strandsSettingToggle.boolValue))
                    {
                        heightNow += EditorGUIUtility.singleLineHeight;

                        if (localStrandsFoldout.boolValue)
                        {
                            heightNow = DrawSerializedPropertyGroup(localStrandsProperties, rect, heightNow);
                            // apply button for strands settings
                            if (GUI.Button(new Rect(rect.width / 2 - 60, heightNow, 200, EditorGUIUtility.singleLineHeight + 5), "Apply"))
                            {
                                hairProfileNeedReload = true;
                            }
                            heightNow += EditorGUIUtility.singleLineHeight + 5;
                        }
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                    EditorGUI.EndDisabledGroup();
                }
                {
                    SerializedProperty[] localGeometryProperties = GetSerializedPropertyGroup(HairSettingGroupNames.geometrySettingNames, localRenderingSettings);
                    SerializedProperty geometrySettingToggle = element.FindPropertyRelative("geometrySettingToggle");
                    geometrySettingToggle.boolValue = EditorGUI.Toggle(new Rect(rect.x - 10, heightNow, 10, EditorGUIUtility.singleLineHeight), geometrySettingToggle.boolValue);
                    localGeometryFoldout.boolValue = EditorGUI.Foldout(new Rect(rect.x + 20, heightNow, rect.width, EditorGUIUtility.singleLineHeight), localGeometryFoldout.boolValue, "Geometry Settings", true);

                    using (new EditorGUI.DisabledScope(!geometrySettingToggle.boolValue))
                    {
                        heightNow += EditorGUIUtility.singleLineHeight;

                        if (localGeometryFoldout.boolValue)
                        {
                            heightNow = DrawSerializedPropertyGroup(localGeometryProperties, rect, heightNow);
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                {
                    SerializedProperty[] localSimulationProperties = GetSerializedPropertyGroup(HairSettingGroupNames.simulationSettingNames, localSimulationSettings);
                    SerializedProperty simulationSettingToggle = element.FindPropertyRelative("simulationSettingToggle");
                    simulationSettingToggle.boolValue = EditorGUI.Toggle(new Rect(rect.x - 10, heightNow, 10, EditorGUIUtility.singleLineHeight), simulationSettingToggle.boolValue);
                    localSimulationFoldout.boolValue = EditorGUI.Foldout(new Rect(rect.x + 20, heightNow, rect.width, EditorGUIUtility.singleLineHeight), localSimulationFoldout.boolValue, "Simulation Settings", true);

                    using (new EditorGUI.DisabledScope(!simulationSettingToggle.boolValue))
                    {
                        heightNow += EditorGUIUtility.singleLineHeight;
                        if (localSimulationFoldout.boolValue)
                        {
                            heightNow = DrawSerializedPropertyGroup(localSimulationProperties, rect, heightNow);
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            };

            // Height
            m_HairReorderableList.elementHeightCallback = (int index) =>
            {
                // Get hair variables
                SerializedProperty element                  = m_HairListProperty.GetArrayElementAtIndex(index);
                SerializedProperty hair                     = element.FindPropertyRelative("hairAsset");
                SerializedProperty localStrandsFoldout      = element.FindPropertyRelative("localStrandsFoldout");
                SerializedProperty localGeometryFoldout     = element.FindPropertyRelative("localGeometryFoldout");
                SerializedProperty localSimulationFoldout   = element.FindPropertyRelative("localSimulationFoldout");

                int customizeHeightMul = (localStrandsFoldout.boolValue ? HairSettingGroupNames.strandsSettingNames.Length + 1 : 0) +
                                         (localGeometryFoldout.boolValue ? HairSettingGroupNames.geometrySettingNames.Length : 0) +
                                         (localSimulationFoldout.boolValue? HairSettingGroupNames.simulationSettingNames.Length : 0);
                if (customizeHeightMul > 0)
                    return EditorGUIUtility.singleLineHeight * (customizeHeightMul + ((EditorGUIUtility.wideMode) ? 6 : 7));
                else
                    return EditorGUIUtility.singleLineHeight * 5;
            };

            // Add Callback
            m_HairReorderableList.onAddCallback = (ReorderableList list) =>
            {
                m_HairListProperty.arraySize++;
            };

            // Change
            m_HairReorderableList.onChangedCallback = (ReorderableList list) => { hairProfileNeedReload = true; };
        }

        SerializedProperty[] GetSerializedPropertyGroup(string[] propertyNames, SerializedProperty serializedSettings)
        {
            SerializedProperty[] serializedProperties = new SerializedProperty[propertyNames.Length];
            for (int i = 0; i < propertyNames.Length; ++i)
            {
                serializedProperties[i] = serializedSettings.FindPropertyRelative(propertyNames[i]);
            }
            return serializedProperties;
        }

        void DrawSerializedPropertyGroup(SerializedProperty[] serializedProperties)
        {
            foreach (SerializedProperty property in serializedProperties)
            {
                propertyGUIContent = HairFXSettingTooltips.Get(property.name);
                if (property != null)
                {
                    SerializedProperty valueProperty = property.FindPropertyRelative("value");
                    propertyValueRange = HairSettingRanges.Get(property.name);
                    switch (valueProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            valueProperty.intValue = EditorGUILayout.IntSlider(propertyGUIContent, valueProperty.intValue, (int)propertyValueRange.x, (int)propertyValueRange.y);
                            break;
                        case SerializedPropertyType.Float:
                            valueProperty.floatValue = EditorGUILayout.Slider(propertyGUIContent, valueProperty.floatValue, propertyValueRange.x, propertyValueRange.y);
                            break;
                        case SerializedPropertyType.Boolean:
                            valueProperty.boolValue = EditorGUILayout.Toggle(propertyGUIContent, valueProperty.boolValue);
                            break;
                        case SerializedPropertyType.Vector2:
                            MinMax = valueProperty.vector2Value;
                            MinMax.x = Mathf.Clamp(MinMax.x, propertyValueRange.x, propertyValueRange.y);
                            MinMax.y = Mathf.Clamp(MinMax.y, MinMax.x, propertyValueRange.y);
                            MinMax = EditorGUILayout.Vector2Field(propertyGUIContent, MinMax);
                            EditorGUILayout.MinMaxSlider(propertyGUIContent.text + " Range", ref MinMax.x, ref MinMax.y, propertyValueRange.x, propertyValueRange.y);
                            valueProperty.vector2Value = MinMax;
                            break;
                        case SerializedPropertyType.Enum:
                            TessellationNumber t = (TessellationNumber)(Enum.GetValues(typeof(TessellationNumber))).GetValue(valueProperty.enumValueIndex);
                            TessellationNumber newT = (TessellationNumber)EditorGUILayout.EnumPopup(propertyGUIContent, t);
                            valueProperty.enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(TessellationNumber)), newT);
                            break;
                        case SerializedPropertyType.AnimationCurve:
                            valueProperty.animationCurveValue = EditorGUILayout.CurveField(propertyGUIContent, valueProperty.animationCurveValue);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        float DrawSerializedPropertyGroup(SerializedProperty[] serializedProperties, Rect rect, float heightNow)
        {
            foreach (SerializedProperty property in serializedProperties)
            {
                propertyGUIContent = HairFXSettingTooltips.Get(property.name);
                if (property != null)
                {
                    //toggle
                    SerializedProperty overrideProperty = property.FindPropertyRelative("overrideStates");
                    overrideProperty.boolValue = EditorGUI.Toggle(new Rect(rect.x - 10, heightNow, 10, EditorGUIUtility.singleLineHeight), overrideProperty.boolValue);

                    using (new EditorGUI.DisabledScope(!overrideProperty.boolValue))
                    {
                        // slider rect
                        propertyRect = new Rect(rect.x + 20f, heightNow, rect.width - 20, EditorGUIUtility.singleLineHeight);
                        SerializedProperty valueProperty = property.FindPropertyRelative("value");
                        propertyValueRange = HairSettingRanges.Get(property.name);

                        switch (valueProperty.propertyType)
                        {
                            case SerializedPropertyType.Integer:
                                valueProperty.intValue = EditorGUI.IntSlider(propertyRect, propertyGUIContent, valueProperty.intValue, (int)propertyValueRange.x, (int)propertyValueRange.y);
                                break;
                            case SerializedPropertyType.Float:
                                valueProperty.floatValue = EditorGUI.Slider(propertyRect, propertyGUIContent, valueProperty.floatValue, propertyValueRange.x, propertyValueRange.y);
                                break;
                            case SerializedPropertyType.Boolean:
                                valueProperty.boolValue = EditorGUI.Toggle(propertyRect, propertyGUIContent, valueProperty.boolValue);
                                break;
                            case SerializedPropertyType.Vector2:
                                MinMax = valueProperty.vector2Value;
                                MinMax.x = Mathf.Clamp(MinMax.x, propertyValueRange.x, propertyValueRange.y);
                                MinMax.y = Mathf.Clamp(MinMax.y, MinMax.x, propertyValueRange.y);
                                MinMax = EditorGUI.Vector2Field(propertyRect, propertyGUIContent, MinMax);
                                heightNow += EditorGUIUtility.singleLineHeight;
                                if (!EditorGUIUtility.wideMode) // detect Vector2 in wideMode
                                {
                                    heightNow += EditorGUIUtility.singleLineHeight;
                                }
                                EditorGUI.MinMaxSlider(new Rect(rect.x + 20f, heightNow, rect.width - 20, EditorGUIUtility.singleLineHeight), propertyGUIContent.text + " Range", ref MinMax.x, ref MinMax.y, propertyValueRange.x, propertyValueRange.y);
                                valueProperty.vector2Value = MinMax;
                                break;
                            case SerializedPropertyType.Enum:
                                TessellationNumber t = (TessellationNumber)(Enum.GetValues(typeof(TessellationNumber))).GetValue(valueProperty.enumValueIndex);
                                TessellationNumber newT = (TessellationNumber)EditorGUI.EnumPopup(propertyRect, propertyGUIContent, t);
                                valueProperty.enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(TessellationNumber)), newT);
                                break;
                            case SerializedPropertyType.AnimationCurve:
                                valueProperty.animationCurveValue = EditorGUI.CurveField(propertyRect, propertyGUIContent, valueProperty.animationCurveValue);
                                break;
                            default:
                                break;
                        }
                    }
                    heightNow += EditorGUIUtility.singleLineHeight;
                }
            }
            return heightNow;
        }

        public override void OnInspectorGUI()
        {
            if (m_SerializedObject == null) return;

            EditorGUI.BeginChangeCheck();
            m_SerializedObject.Update(); // to representation

            hairStatusFoldout.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(hairStatusFoldout.boolValue, "Hair Status");
            if (hairStatusFoldout.boolValue)
            {
                m_TFXHairProfile.LoadAllHairDataHeader();
                EditorGUILayout.LabelField("Guide Strands", "" + m_TFXHairProfile.getNumGuideStrands());
                EditorGUILayout.LabelField("Guide Vertices", "" + m_TFXHairProfile.getNumGuideVertices());
                EditorGUILayout.LabelField("Total Strands", "" + m_TFXHairProfile.getNumTotalStrands());
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            // global stands settings
            strandsSettingFoldout.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(strandsSettingFoldout.boolValue, "Global Strands Settings");
            if (strandsSettingFoldout.boolValue)
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);

                DrawSerializedPropertyGroup(globalStrandsSerializedProperties);
                // Apply button
                //if (GUILayout.Button("Apply", GUILayout.MaxWidth(200))) hairProfileNeedReload = true;
                EditorGUILayout.Space(5);
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x = rect.width / 2 - 80;
                rect.width = 200;
                rect.height = EditorGUIUtility.singleLineHeight + 5;
                if (GUI.Button(rect, "Apply")) hairProfileNeedReload = true;

                EditorGUILayout.Space(20);
                EditorGUI.EndDisabledGroup();

                if (Application.isPlaying)
                    EditorGUILayout.HelpBox("Not editable in Play mode.", MessageType.Info);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // global geometry settings
            geometrySettingFoldout.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(geometrySettingFoldout.boolValue, "Global Geometry Settings");
            if (geometrySettingFoldout.boolValue)
            {
                DrawSerializedPropertyGroup(globalGeometrySerializedProperties);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // global simulation settings
            simulationSettingFoldout.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(simulationSettingFoldout.boolValue, "Global Simulation Settings");
            if (simulationSettingFoldout.boolValue)
            {
                DrawSerializedPropertyGroup(globalSimulationSerializedProperties);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            m_HairReorderableList.DoLayoutList();

            m_SerializedObject.ApplyModifiedProperties(); // to properties

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_TFXHairProfile);
                Repaint();
            }

        }

    }
}
