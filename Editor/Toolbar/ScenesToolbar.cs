#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [InitializeOnLoad]
    public static class ScenesToolbar
    {
        private static int currentPath = 0;
        private static int lastPath = 0;
        private readonly static List<string> scenes;
        private readonly static List<string> sceneNames;

        static ScenesToolbar()
        {
            ToolbarExtension.LeftToolbarGUI.Add(OnToolbarGUI);

            scenes = new();
            sceneNames = new();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                scenes.Add(scene.path);
                sceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
            }

            currentPath = scenes.IndexOf(EditorSceneManager.GetActiveScene().path);
            lastPath = currentPath;            
        }

        private static void OnToolbarGUI()
        {
            if (EditorApplication.isPlaying) GUI.enabled = false;
            GUILayout.Space(5);
            currentPath = EditorGUILayout.Popup(currentPath, sceneNames.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100f));
            if (currentPath != lastPath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    lastPath = currentPath;
                    EditorSceneManager.OpenScene(scenes[currentPath]);
                }
                else currentPath = lastPath;
            }
            GUI.enabled = true;
        }
    }
}
#endif