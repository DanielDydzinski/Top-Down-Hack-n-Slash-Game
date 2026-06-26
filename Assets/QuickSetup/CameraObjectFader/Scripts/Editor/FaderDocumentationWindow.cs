using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CameraObjectFader.Internal;

namespace CameraObjectFader.Editor
{
    public class FaderDocumentationWindow : EditorWindow
    {
        private GUIStyle headerStyle;
        private GUIStyle sectionTitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle boxStyle;

        private Vector2 scrollPosition;

        private const string TP_SCENE_PATH = "Assets/QuickSetup/CameraObjectFader/Demo/Scene/DemoScene Third Person.unity";


        [MenuItem("Tools/QuickSetup/Camera Object Fader")]
        public static void ShowWindow()
        {
            FaderDocumentationWindow window = GetWindow<FaderDocumentationWindow>(true, "Camera Object Fader", true);
            Vector2 size = new Vector2(400, 600);
            window.minSize = size;
            window.maxSize = size;
            window.Show();
        }

        private void OnEnable()
        {
            // InitStyles must be called from OnGUI
        }

        private void OnGUI()
        {
            InitStyles();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // --- Header Banner ---
            EditorGUILayout.Space(10);
            Rect headerRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(headerRect, "", boxStyle);
            GUI.Label(headerRect, "CAMERA OBJECT FADER", headerStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Documentation & Quick Start Guide", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // --- Section: Introduction ---
            DrawSectionTitle("INTRODUCTION");
            EditorGUILayout.LabelField("Camera Object Fader is a high-performance solution for handling occluding objects in 3D games. It automatically detects and fades out objects that block the view between your camera and the target.", bodyStyle);

            EditorGUILayout.Space(15);

            // --- Section: Key Features ---
            DrawSectionTitle("KEY FEATURES");
            DrawBulletPoint("High Performance: NonAlloc physics & centralized updates.");
            DrawBulletPoint("Multi-Pipeline: Supports Standard, URP, and HDRP.");
            DrawBulletPoint("Distance Scaling: Dynamic alpha based on distance.");
            DrawBulletPoint("Exclusion Lists: Filter by Tags or specific Objects.");
            DrawBulletPoint("Callbacks: UnityEvents for obstruction start/end.");

            EditorGUILayout.Space(15);

            // --- Section: Quick Start ---
            DrawSectionTitle("QUICK START");
            EditorGUILayout.BeginVertical(boxStyle);
            DrawStep(1, "Attach 'CameraFadeController' to your Main Camera.");
            DrawStep(2, "Assign your Player to the 'Target' field.");
            DrawStep(3, "Configure the 'Fade Layer' mask to include walls/environment.");
            DrawStep(4, "Press Play! The system handles everything automatically.");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // --- Section: Occlusion Culling ---
            DrawSectionTitle("OCCLUSION CULLING TIP");
            EditorGUILayout.LabelField("If you use Unity's built-in Occlusion Culling, avoid marking 'fadeable' walls as Static Occluders. If a wall is culled as a static occluder, objects behind it will remain invisible even if the wall fades out. For best results, keep your fadeable environment as Occludees only.", bodyStyle);

            EditorGUILayout.Space(20);

            // --- Footer: Demo Scenes ---
            DrawSectionTitle("DEMO SCENES");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Third Person Demo", GUILayout.Height(30)))
            {
                OpenScene(TP_SCENE_PATH);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // --- Footer: External Links ---
            DrawSectionTitle("ONLINE RESOURCES");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌐 Web Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL(FaderConstants.DOC_URL);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);
            GUI.enabled = false;
            EditorGUILayout.LabelField($"Version {FaderConstants.VERSION} | Advanced Fader System", EditorStyles.centeredGreyMiniLabel);
            GUI.enabled = true;
            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        private void InitStyles()
        {
            if (headerStyle != null && sectionTitleStyle != null && bodyStyle != null && boxStyle != null) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.8f, 1f) : new Color(0f, 0.4f, 0.6f);

            sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 10, 5)
            };
            sectionTitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            bodyStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 12
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        private void DrawSectionTitle(string title)
        {
            EditorGUILayout.LabelField(title, sectionTitleStyle);
            Rect line = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.7f, 0.7f, 0.7f));
            EditorGUILayout.Space(5);
        }

        private void DrawBulletPoint(string text)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(15));
            EditorGUILayout.LabelField(text, bodyStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStep(int stepnum, string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.LabelField(stepnum.ToString() + ".", GUILayout.Width(20));
            GUI.enabled = true;
            EditorGUILayout.LabelField(text, bodyStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
    }
}
