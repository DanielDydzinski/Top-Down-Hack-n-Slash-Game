using UnityEngine;
using UnityEditor;
using CameraObjectFader;

namespace CameraObjectFader.Editor
{
    [CustomEditor(typeof(FadeableObject))]
    public class FadeableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var script = (FadeableObject)target;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("This object is managed by the CameraFadeController. It handles the material instancing and smooth alpha transitions.", MessageType.Info);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            base.OnInspectorGUI();

            EditorGUILayout.Space(5);
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Force Re-Initialize Renderers"))
                {
                    script.Initialize();
                    EditorUtility.SetDirty(script);
                    Debug.Log("FadeableObject: Renderers re-initialized.");
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.LabelField("Status: " + (script.IsFullyOpaque ? "Opaque" : "Fading/Transparent"), EditorStyles.boldLabel);
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
