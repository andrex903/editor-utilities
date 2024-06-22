#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedeevEditor.Utilities
{
    [InitializeOnLoad]
    public static class ScenesToolbar
    {
        private static int activeSceneIndex = 0;
        private static int lastSceneIndex = -1;

        private readonly static List<string> scenes;
        private readonly static List<string> sceneNames;

        static ScenesToolbar()
        {
            ToolbarExtension.LeftToolbarGUI.Add(OnToolbarGUI);

            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            scenes = new();
            sceneNames = new();
            FindAllScenes();
        }

        private static void OnSceneChanged(Scene _, Scene scene)
        {
            int index = scenes.IndexOf(scene.path);
            if (index >= 0 && index < scenes.Count)
            {
                activeSceneIndex = index;
            }
        }

        private static void OnToolbarGUI()
        {
            if (EditorApplication.isPlaying) GUI.enabled = false;
            GUILayout.Space(5);

            activeSceneIndex = EditorGUILayout.Popup(activeSceneIndex, sceneNames.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(150f));
            if (activeSceneIndex != lastSceneIndex)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    lastSceneIndex = activeSceneIndex;
                    EditorSceneManager.OpenScene(scenes[activeSceneIndex]);
                }
                else activeSceneIndex = lastSceneIndex;
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
                scenes.Add(path);
                sceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }
        }
    }
}
#endif