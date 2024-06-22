#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedeevEditor.Utilities
{
    [InitializeOnLoad()]
    public static class ScenesToolbar
    {
        private readonly static List<string> scenes;
        private readonly static List<string> sceneNames;

        private static int ActiveSceneIndex
        {
            get
            {
                return SessionState.GetInt("ActiveSceneIndex", sceneNames.IndexOf(EditorSceneManager.GetActiveScene().name));
            }

            set
            {
                SessionState.SetInt("ActiveSceneIndex", value);
            }
        }

        private static int LastSceneIndex
        {
            get
            {
                return SessionState.GetInt("LastSceneIndex", sceneNames.IndexOf(EditorSceneManager.GetActiveScene().name));
            }

            set
            {
                SessionState.SetInt("LastSceneIndex", value);
            }
        }

        static ScenesToolbar()
        {
            if (EditorApplication.isPlaying) return;

            scenes = new();
            sceneNames = new();
            FindAllScenes();

            ToolbarExtension.LeftToolbarGUI.Add(OnToolbarGUI);
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode != OpenSceneMode.Single) return;

            int index = scenes.IndexOf(scene.path);
            if (index >= 0 && index < scenes.Count)
            {
                ActiveSceneIndex = index;
            }
        }

        private static void OnToolbarGUI()
        {
            if (EditorApplication.isPlaying) GUI.enabled = false;

            GUILayout.Space(5);

            ActiveSceneIndex = EditorGUILayout.Popup(ActiveSceneIndex, sceneNames.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(150f));
            if (ActiveSceneIndex != LastSceneIndex)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    LastSceneIndex = ActiveSceneIndex;
                    EditorSceneManager.OpenScene(scenes[ActiveSceneIndex]);
                }
                else ActiveSceneIndex = LastSceneIndex;
            }

            GUILayout.Space(2);

            if (EditorUtilityGUI.IconButton("d_Refresh@2x", 30f, EditorStyles.toolbarButton, "Refresh Scenes"))
            {
                FindAllScenes();
            }

            GUI.enabled = true;
        }

        private static void FindAllScenes()
        {
            scenes.Clear();
            sceneNames.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/Scenes" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                scenes.Add(path);
                sceneNames.Add(sceneName);
            }
        }
    }
}
#endif