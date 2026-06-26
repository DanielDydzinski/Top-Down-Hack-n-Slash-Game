using UnityEngine;
using UnityEditor;

namespace CameraObjectFader.Editor
{
    [InitializeOnLoad]
    public static class FaderStartup
    {
        private const string SHOW_DOC_KEY = "CameraObjectFader.ShowDocOnImport";

        static FaderStartup()
        {
            EditorApplication.delayCall += CheckAndShowDocumentation;
        }

        private static void CheckAndShowDocumentation()
        {
            if (EditorPrefs.GetBool(SHOW_DOC_KEY, true))
            {
                FaderDocumentationWindow.ShowWindow();
                Debug.Log("<color=cyan><b>[Camera Object Fader]</b></color> Welcome! Opening documentation for setup guide.");
                EditorPrefs.SetBool(SHOW_DOC_KEY, false);
            }
        }
    }
}
