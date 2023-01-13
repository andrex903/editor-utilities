#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class Selector : EditorWindow
    {
        private string tag = "Untagged";
        private LayerMask layerMask;

        private const float heightSize = 225f;
        private const float widthSize = 300f;

        [MenuItem("Tools/Utilities/Selector")]
        public static void ShowWindow()
        {
            var window = GetWindow<Selector>(title: "Selector");
            window.minSize = new Vector2(widthSize, heightSize);
        }

        private void OnEnable()
        {
            position = new(Screen.width / 2 - widthSize / 2, Screen.height / 2 - heightSize / 2, widthSize, heightSize);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("By Tag:", EditorStyles.boldLabel);
            tag = EditorGUILayout.TagField("", tag);
            if (GUILayout.Button("Select")) SelectObjectsWithTag();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("By Layer:", EditorStyles.boldLabel);
            layerMask = EditorGUILayout.LayerField("", layerMask);
            if (GUILayout.Button("Select")) SelectObjectsWithLayer();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Other:", EditorStyles.boldLabel);
            if (GUILayout.Button("Select Negative Scale")) SelectObjectsWithNegativeScale();
            if (GUILayout.Button("Deselect All")) Selection.objects = new Object[0];
            EditorGUILayout.EndVertical();
        }

        private void DisplayResults()
        {
            Debug.Log($"{Selection.count} objects found!");
        }

        private void SelectObjectsWithTag()
        {
            if (string.IsNullOrEmpty(tag)) return;

            Selection.objects = GameObject.FindGameObjectsWithTag(tag);
            DisplayResults();
        }

        private void SelectObjectsWithNegativeScale()
        {
            List<GameObject> list = new();
            foreach (var obj in FindObjectsOfType<GameObject>())
            {
                if (obj.transform.localScale.x < 0f || obj.transform.localScale.y < 0f || obj.transform.localScale.z < 0f)
                {
                    list.Add(obj);
                }
            }
            Selection.objects = list.ToArray();
            DisplayResults();
        }

        private void SelectObjectsWithLayer()
        {
            List<GameObject> list = new();
            foreach (var obj in FindObjectsOfType<GameObject>())
            {
                if (obj.layer == layerMask.value) list.Add(obj);
            }
            Selection.objects = list.ToArray();
            DisplayResults();
        }
    }
}
#endif