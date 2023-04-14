#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [InitializeOnLoad]
    public static class PlayerToolbar
    {
        static PlayerToolbar()
        {
            ToolbarExtension.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(5f);
            if (EditorUtilityGUI.IconButton("BodySilhouette", 30f, EditorStyles.toolbarButton, "Select Player"))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player)
                {
                    Selection.activeGameObject = player;
                }
            }
            if (EditorUtilityGUI.IconButton("d_Search Icon", 30f, EditorStyles.toolbarButton, "Center on Player"))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player)
                {
                    SceneView.lastActiveSceneView.LookAt(player.transform.position);
                    SceneView.lastActiveSceneView.size = 5;
                }
            }
        }
    }
}
#endif