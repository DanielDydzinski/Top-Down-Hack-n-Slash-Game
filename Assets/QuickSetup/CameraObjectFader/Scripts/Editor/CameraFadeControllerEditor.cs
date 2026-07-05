using UnityEngine;
using UnityEditor;
using CameraObjectFader;
using CameraObjectFader.Internal;

namespace CameraObjectFader.Editor
{
    [CustomEditor(typeof(CameraFadeController))]
    public class CameraFadeControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty targetsProp;
        private SerializedProperty targetOffsetProp;
        private SerializedProperty fadeLayerProp;
        private SerializedProperty fadeAlphaProp;
        private SerializedProperty fadeSpeedProp;
        private SerializedProperty useSphereCastProp;
        private SerializedProperty castRadiusProp;
        private SerializedProperty minDistanceProp;
        private SerializedProperty maxFadeDistanceProp;
        private SerializedProperty fadeChildrenRenderersProp;

        private SerializedProperty useDistanceScalingProp;
        private SerializedProperty nearAlphaProp;
        private SerializedProperty farAlphaProp;
        private SerializedProperty ignoreTagsProp;
        private SerializedProperty ignoreObjectsProp;
        private SerializedProperty onObstructionStartProp;
        private SerializedProperty onObstructionEndProp;

        // ── Mouse Circle Mask ─────────────────────────────────────────────
        private SerializedProperty mouseWorldTransformProp;
        private SerializedProperty mouseCircleRadiusProp;
        private SerializedProperty mouseCircleFeatherProp;
        private bool showMouseCircle = true;

        private bool showTargetSettings = true;
        private bool showTargetFading = true;
        private bool showFadeSettings = true;
        private bool showDistanceScaling = true;
        private bool showExclusionSettings = true;
        private bool showTagsList = true;
        private bool showObjectsList = true;
        private bool showCollisionSettings = true;
        private bool showEvents = true;

        private SerializedProperty fadeTargetWhenCloseProp;
        private SerializedProperty targetFadeStartDistanceProp;
        private SerializedProperty targetFadeEndDistanceProp;

        private void OnEnable()
        {
            onObstructionStartProp = serializedObject.FindProperty("onObstructionStart");
            onObstructionEndProp = serializedObject.FindProperty("onObstructionEnd");
            targetsProp = serializedObject.FindProperty("targets");
            targetOffsetProp = serializedObject.FindProperty("targetOffset");
            fadeLayerProp = serializedObject.FindProperty("fadeLayer");
            fadeAlphaProp = serializedObject.FindProperty("fadeAlpha");
            fadeSpeedProp = serializedObject.FindProperty("fadeSpeed");
            useSphereCastProp = serializedObject.FindProperty("useSphereCast");
            castRadiusProp = serializedObject.FindProperty("castRadius");
            minDistanceProp = serializedObject.FindProperty("minDistance");
            maxFadeDistanceProp = serializedObject.FindProperty("maxFadeDistance");
            fadeChildrenRenderersProp = serializedObject.FindProperty("fadeChildrenRenderers");

            useDistanceScalingProp = serializedObject.FindProperty("useDistanceScaling");
            nearAlphaProp = serializedObject.FindProperty("nearAlpha");
            farAlphaProp = serializedObject.FindProperty("farAlpha");
            ignoreTagsProp = serializedObject.FindProperty("ignoreTags");
            ignoreObjectsProp = serializedObject.FindProperty("ignoreObjects");

            fadeTargetWhenCloseProp = serializedObject.FindProperty("fadeTargetWhenClose");
            targetFadeStartDistanceProp = serializedObject.FindProperty("targetFadeStartDistance");
            targetFadeEndDistanceProp = serializedObject.FindProperty("targetFadeEndDistance");

            // Mouse Circle Mask
            mouseWorldTransformProp = serializedObject.FindProperty("mouseWorldTransform");
            mouseCircleRadiusProp = serializedObject.FindProperty("mouseCircleRadius");
            mouseCircleFeatherProp = serializedObject.FindProperty("mouseCircleFeather");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header Banner
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            EditorGUILayout.Space(10);
            Rect rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "", EditorStyles.helpBox);
            GUI.Label(rect, "CAMERA OBJECT FADER", headerStyle);
            EditorGUILayout.Space(5);

            // Target Area
            showTargetSettings = EditorGUILayout.Foldout(showTargetSettings, "TARGETING CONFIGURATION", true, EditorStyles.foldoutHeader);
            if (showTargetSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (int i = 0; i < targetsProp.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(targetsProp.GetArrayElementAtIndex(i), new GUIContent($"Target {i + 1}"));
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        targetsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("+ Add Target", EditorStyles.miniButton))
                    targetsProp.arraySize++;

                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(targetOffsetProp);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Target Fading
            showTargetFading = EditorGUILayout.Foldout(showTargetFading, "TARGET FADING", true, EditorStyles.foldoutHeader);
            if (showTargetFading)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(fadeTargetWhenCloseProp);
                if (fadeTargetWhenCloseProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    targetFadeStartDistanceProp.floatValue = EditorGUILayout.Slider("Fade Start Distance", targetFadeStartDistanceProp.floatValue, 0f, 10f);
                    targetFadeEndDistanceProp.floatValue = EditorGUILayout.Slider("Fade End Distance", targetFadeEndDistanceProp.floatValue, 0f, 10f);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Fade Settings
            showFadeSettings = EditorGUILayout.Foldout(showFadeSettings, "BASIC FADE SETTINGS", true, EditorStyles.foldoutHeader);
            if (showFadeSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                fadeAlphaProp.floatValue = EditorGUILayout.Slider("Base Fade Alpha", fadeAlphaProp.floatValue, 0f, 1f);
                fadeSpeedProp.floatValue = EditorGUILayout.Slider("Fade Speed", fadeSpeedProp.floatValue, 0.1f, 20f);
                EditorGUILayout.PropertyField(fadeChildrenRenderersProp, new GUIContent("Fade Children", "Should renderers in child objects also fade?"));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Distance Scaling
            showDistanceScaling = EditorGUILayout.Foldout(showDistanceScaling, "DISTANCE SCALING", true, EditorStyles.foldoutHeader);
            if (showDistanceScaling)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(useDistanceScalingProp);
                if (useDistanceScalingProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    nearAlphaProp.floatValue = EditorGUILayout.Slider("Near Alpha (Cam)", nearAlphaProp.floatValue, 0f, 1f);
                    farAlphaProp.floatValue = EditorGUILayout.Slider("Far Alpha (Target)", farAlphaProp.floatValue, 0f, 1f);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Exclusion Settings
            showExclusionSettings = EditorGUILayout.Foldout(showExclusionSettings, "EXCLUSION / IGNORE LIST", true, EditorStyles.foldoutHeader);
            if (showExclusionSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                showTagsList = EditorGUILayout.Foldout(showTagsList, "Ignore by Tags", true);
                if (showTagsList)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < ignoreTagsProp.arraySize; i++)
                    {
                        SerializedProperty element = ignoreTagsProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.BeginHorizontal();
                        element.stringValue = EditorGUILayout.TagField($"Slot {i}", element.stringValue);
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            ignoreTagsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("+ Add New Tag", EditorStyles.miniButton))
                    {
                        ignoreTagsProp.arraySize++;
                        ignoreTagsProp.GetArrayElementAtIndex(ignoreTagsProp.arraySize - 1).stringValue = "Untagged";
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);

                showObjectsList = EditorGUILayout.Foldout(showObjectsList, "Ignore Specific Objects", true);
                if (showObjectsList)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < ignoreObjectsProp.arraySize; i++)
                    {
                        SerializedProperty element = ignoreObjectsProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(element, new GUIContent($"Slot {i}"));
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            ignoreObjectsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("+ Add New Object", EditorStyles.miniButton))
                    {
                        ignoreObjectsProp.arraySize++;
                        ignoreObjectsProp.GetArrayElementAtIndex(ignoreObjectsProp.arraySize - 1).objectReferenceValue = null;
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Collision / Detection
            showCollisionSettings = EditorGUILayout.Foldout(showCollisionSettings, "DETECTION SETTINGS", true, EditorStyles.foldoutHeader);
            if (showCollisionSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(fadeLayerProp);

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(useSphereCastProp);
                if (useSphereCastProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    castRadiusProp.floatValue = EditorGUILayout.Slider("Cast Radius", castRadiusProp.floatValue, 0.01f, 5f);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);
                minDistanceProp.floatValue = EditorGUILayout.Slider("Min Fade Distance", minDistanceProp.floatValue, 0f, 10f);
                maxFadeDistanceProp.floatValue = EditorGUILayout.Slider("Max Fade Distance", maxFadeDistanceProp.floatValue, 1f, 100f);

                EditorGUILayout.HelpBox("Red WireSphere in Scene view shows the Max Fade Distance range.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // ── Mouse Circle Mask ─────────────────────────────────────────
            showMouseCircle = EditorGUILayout.Foldout(showMouseCircle, "MOUSE CIRCLE MASK", true, EditorStyles.foldoutHeader);
            if (showMouseCircle)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.PropertyField(mouseWorldTransformProp,
                    new GUIContent("Mouse World Transform",
                        "The transform PlayerToMouse moves to the mouse floor position.\n" +
                        "Leave empty to disable the mouse circle entirely."));

                if (mouseWorldTransformProp.objectReferenceValue != null)
                {
                    EditorGUI.indentLevel++;
                    mouseCircleRadiusProp.floatValue = EditorGUILayout.Slider(
                        new GUIContent("Circle Radius", "Radius of the reveal circle in screen pixels."),
                        mouseCircleRadiusProp.floatValue, 10f, 600f);

                    mouseCircleFeatherProp.floatValue = EditorGUILayout.Slider(
                        new GUIContent("Feather Width", "Soft edge width in screen pixels."),
                        mouseCircleFeatherProp.floatValue, 0f, 200f);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.HelpBox(
                        "The mouse circle only appears when the cursor is over an object " +
                        "that is already fading the player's view this frame.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Assign a Transform to enable the mouse circle mask.",
                        MessageType.None);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Events
            showEvents = EditorGUILayout.Foldout(showEvents, "CALLBACK EVENTS", true, EditorStyles.foldoutHeader);
            if (showEvents)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(onObstructionStartProp);
                EditorGUILayout.PropertyField(onObstructionEndProp);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(15);

            GUI.enabled = false;
            EditorGUILayout.LabelField($"Version {FaderConstants.VERSION}", EditorStyles.miniLabel);
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}