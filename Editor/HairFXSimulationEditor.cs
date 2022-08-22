using UnityEditor;
using UnityEngine;

namespace HairFX
{
    [CustomEditor(typeof(HairFXSimulation))]
    [CanEditMultipleObjects]
    public class HairFXSimulationEditor : Editor
    {
        HairFXSimulation m_HairSimulation;
        static bool windFoldout = false;
        static bool debugFoldout = false;

        SerializedProperty enableSimulation;
        SerializedProperty frameLimit;
        //SerializedProperty parentTransform;
        SerializedProperty speedLimit;
        SerializedProperty resetDistance;
        SerializedProperty collision;
        SerializedProperty capsuleColliders;
        SerializedProperty useWindZone;
        SerializedProperty windZone;
        SerializedProperty windDirection;
        SerializedProperty windConeAngle;
        SerializedProperty windMagnitude;
        SerializedProperty windTurbulence;
        SerializedProperty pulseMagnitude;
        SerializedProperty pulseFrequency;
        SerializedProperty doIntegrationAndGlobalShapeConstraints;
        SerializedProperty doVSP;
        SerializedProperty doLocalShapeConstraints;
        SerializedProperty doLengthConstraintsWindAndCollision;
        SerializedProperty followHairs;

        void OnEnable()
        {
            if (target == null) return;
            m_HairSimulation = (HairFXSimulation)target;

            enableSimulation                        = serializedObject.FindProperty("enableSimulation");
            frameLimit                              = serializedObject.FindProperty("frameLimit");
            //parentTransform                         = serializedObject.FindProperty("parentTransform");
            speedLimit                              = serializedObject.FindProperty("speedLimit");
            resetDistance                           = serializedObject.FindProperty("resetDistance");
            collision                               = serializedObject.FindProperty("collision");
            capsuleColliders                        = serializedObject.FindProperty("capsuleColliders");
            useWindZone                             = serializedObject.FindProperty("useWindZone");
            windZone                                = serializedObject.FindProperty("windZone");
            windDirection                           = serializedObject.FindProperty("windDirection");
            windConeAngle                           = serializedObject.FindProperty("windConeAngle");
            windMagnitude                           = serializedObject.FindProperty("windMagnitude");
            windTurbulence                          = serializedObject.FindProperty("windTurbulence");
            pulseMagnitude                          = serializedObject.FindProperty("pulseMagnitude");
            pulseFrequency                          = serializedObject.FindProperty("pulseFrequency");
            doIntegrationAndGlobalShapeConstraints  = serializedObject.FindProperty("doIntegrationAndGlobalShapeConstraints");
            doVSP                                   = serializedObject.FindProperty("doVSP");
            doLocalShapeConstraints                 = serializedObject.FindProperty("doLocalShapeConstraints");
            doLengthConstraintsWindAndCollision     = serializedObject.FindProperty("doLengthConstraintsWindAndCollision");
            followHairs                             = serializedObject.FindProperty("followHairs");
        }

        public override void OnInspectorGUI()
        {
            if (m_HairSimulation == null) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(enableSimulation);
            EditorGUILayout.PropertyField(frameLimit);

            EditorGUI.BeginChangeCheck();
            //EditorGUILayout.PropertyField(parentTransform);
            if (EditorGUI.EndChangeCheck())
            {
                m_HairSimulation.UpdateConstants();
            }

            EditorGUILayout.PropertyField(speedLimit, new GUIContent("Move Speed Limit"));

            EditorGUILayout.PropertyField(resetDistance);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(collision);
            using (new EditorGUI.DisabledScope(!collision.boolValue))
            {
                EditorGUILayout.PropertyField(capsuleColliders);
            }

            EditorGUILayout.Space();

            windFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(windFoldout, "Wind");
            if (windFoldout)
            {
                EditorGUILayout.PropertyField(useWindZone);
                if (m_HairSimulation.useWindZone == true)
                {
                    EditorGUILayout.PropertyField(windZone);
                }
                else
                {
                    EditorGUILayout.PropertyField(windDirection);
                    EditorGUILayout.PropertyField(windConeAngle);
                    EditorGUILayout.PropertyField(windMagnitude);
                    EditorGUILayout.PropertyField(windTurbulence);
                    EditorGUILayout.PropertyField(pulseMagnitude);
                    EditorGUILayout.PropertyField(pulseFrequency);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(debugFoldout, "Debug Simulation");
            if (debugFoldout)
            {
                EditorGUILayout.PropertyField(doIntegrationAndGlobalShapeConstraints);
                EditorGUILayout.PropertyField(doVSP);
                EditorGUILayout.PropertyField(doLocalShapeConstraints);
                EditorGUILayout.PropertyField(doLengthConstraintsWindAndCollision);
                EditorGUILayout.PropertyField(followHairs);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}