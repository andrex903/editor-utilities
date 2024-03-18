#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [Serializable]
    public class FastRename : EditorWindow
    {
        private UnityEngine.Object[] selectedObjects = new UnityEngine.Object[0];
        private readonly List<string> previewSelectedObjects = new();

        private bool usebasename;
        private string basename;
        private bool useprefix;
        private string prefix;
        private bool usesuffix;
        private string suffix;

        private bool useRemoveCharacters;
        private int startCount;
        private int endCount;

        private bool usenumbered;
        private int basenumbered = 0;
        private int stepNumbered = 1;

        private bool usereplace;
        private string replace;
        private string replacewith;

        private bool useremove;
        private string remove;

        private bool showselection;

        [MenuItem("Tools/Utilities/Fast Rename")]
        public static void ShowWindow()
        {
            GetWindow<FastRename>("Fast Rename");
        }

        #region GUI

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            usebasename = EditorGUILayout.Toggle(usebasename, GUILayout.MaxWidth(16));
            basename = EditorGUILayout.TextField("Base Name: ", basename);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            useprefix = EditorGUILayout.Toggle(useprefix, GUILayout.MaxWidth(16));
            prefix = EditorGUILayout.TextField("Prefix: ", prefix);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            usesuffix = EditorGUILayout.Toggle(usesuffix, GUILayout.MaxWidth(16));
            suffix = EditorGUILayout.TextField("Suffix: ", suffix);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            usenumbered = EditorGUILayout.Toggle(usenumbered, GUILayout.MaxWidth(16));
            EditorGUILayout.PrefixLabel("Numbered: ");
            EditorGUILayout.BeginVertical();
            basenumbered = EditorGUILayout.IntField("Start number: ", basenumbered);
            stepNumbered = EditorGUILayout.IntField("Step: ", stepNumbered);          
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            usereplace = EditorGUILayout.Toggle(usereplace, GUILayout.MaxWidth(16));
            EditorGUILayout.PrefixLabel("Replace contents: ");
            EditorGUILayout.BeginVertical();

            replace = EditorGUILayout.TextField("Replace: ", replace);
            replacewith = EditorGUILayout.TextField("With: ", replacewith);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            useremove = EditorGUILayout.Toggle(useremove, GUILayout.MaxWidth(16));
            remove = EditorGUILayout.TextField("Remove all: ", remove);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            useRemoveCharacters = EditorGUILayout.Toggle(useRemoveCharacters, GUILayout.MaxWidth(16));
            EditorGUILayout.PrefixLabel("Remove at: ");
            EditorGUILayout.BeginVertical();
            startCount = EditorGUILayout.IntField("From Start: ", startCount);
            endCount = EditorGUILayout.IntField("From End: ", endCount);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            //Rename
            if (GUILayout.Button(new GUIContent("Rename", "Renames selected objects with current settings."))) { Rename(); }
            EditorGUILayout.EndVertical();

            if (selectedObjects.Length > 0)
            {
                showselection = EditorGUILayout.Foldout(showselection, "Selection and preview");
                if (showselection)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Selection", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    for (int i = 0; i < selectedObjects.Length; i++)
                    {
                        EditorGUILayout.LabelField(selectedObjects[i].name);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Preview", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    for (int i = 0; i < selectedObjects.Length; i++)
                    {
                        EditorGUILayout.LabelField(previewSelectedObjects[i]);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button(new GUIContent("Clear settings", "Renames selected objects with current settings.")))
            {
                ClearSettings();
            }
        }

        #endregion

        #region Functions
        private void Update()
        {
            selectedObjects = Selection.objects;

            previewSelectedObjects.Clear();

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                previewSelectedObjects.Add(Rename(selectedObjects[i].name, i));
            }

        }

        private void Rename()
        {
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                Undo.RecordObject(selectedObjects[i], "Rename");

                Rename(selectedObjects[i].name, i);

                string path = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.RenameAsset(path, selectedObjects[i].name);
                }

            }
        }

        private string Rename(string name, int index)
        {
            if (usebasename)
            {
                name = basename;
            }
            if (useprefix)
            {
                name = prefix + name;
            }
            if (usesuffix)
            {
                name += suffix;
            }

            if (usenumbered)
            {
                name += ((basenumbered + (stepNumbered * index)).ToString());
            }

            if (useremove && remove != string.Empty)
            {
                name = name.Replace(remove, string.Empty);
            }

            if (usereplace && replace != string.Empty)
            {
                name = name.Replace(replace, replacewith);
            }

            if (useRemoveCharacters)
            {
                name = RemoveCharacters(name, startCount, endCount);
            }

            return name;
        }

        private static string RemoveCharacters(string input, int start, int end)
        {
            if (start < 0 || end < 0)
            {
                return input;
            }

            if (start >= input.Length || end >= input.Length)
            {
                return input;
            }

            return input.Substring(start, input.Length - start - end);
        }

        private void ClearSettings()
        {
            usebasename = false;
            basename = "";
            useprefix = false;
            prefix = "";
            usesuffix = false;
            suffix = "";
            usenumbered = false;
            basenumbered = 0;
            stepNumbered = 1;

            usereplace = false;
            replace = "";
            replacewith = "";

            useremove = false;
            remove = "";
        }

        #endregion
    }
}
#endif