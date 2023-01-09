#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace RedeevEditor.Utilities
{
    [Serializable]
    public class FastRename : EditorWindow
    {
        private UnityEngine.Object[] SelectedObjects = new UnityEngine.Object[0];
        private GameObject[] SelectedGameObjectObjects = new GameObject[0];

        private string[] PreviewSelectedObjects = new string[0];

        private bool usebasename;
        private string basename;
        private bool useprefix;
        private string prefix;
        private bool usesuffix;
        private string suffix;

        public enum Method
        {
            BySelection = 0,
            ByHierarchy = 1
        }
        public Method method;
        private bool usenumbered;
        private int basenumbered = 0;
        private int stepnumbered = 1;

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
            stepnumbered = EditorGUILayout.IntField("Step: ", stepnumbered);
            method = (Method)EditorGUILayout.EnumPopup(new GUIContent("Number method", "Number by position in selection, or number by hierarchy position. Note: Project files cannot be renamed with the hierarchy method as they are not present in the scene."), method);
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
            //Rename
            if (GUILayout.Button(new GUIContent("Rename", "Renames selected objects with current settings."))) { Rename(); }
            EditorGUILayout.EndVertical();

            if (SelectedObjects.Length > 0)
            {
                showselection = EditorGUILayout.Foldout(showselection, "Selection and preview");
                if (showselection)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Selection", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    for (int i = 0; i < SelectedObjects.Length; i++)
                    {
                        EditorGUILayout.LabelField(SelectedObjects[i].name);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Preview", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    for (int i = 0; i < SelectedObjects.Length; i++)
                    {
                        EditorGUILayout.LabelField(PreviewSelectedObjects[i]);
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
            SelectedObjects = Selection.objects;

            SelectedGameObjectObjects = Selection.gameObjects;

            PreviewSelectedObjects = new string[SelectedObjects.Length];

            for (int i = 0; i < SelectedObjects.Length; i++)
            {
                string str = SelectedObjects[i].name;
                if (usebasename) { str = basename; }
                if (useprefix) { str = prefix + str; }
                if (usesuffix) { str = str + suffix; }

                if (usenumbered && method == Method.BySelection) { str = str + ((basenumbered + (stepnumbered * i)).ToString()); }

                if (useremove && remove != "") { str = str.Replace(remove, ""); }
                if (usereplace && replace != "") { str = str.Replace(replace, replacewith); }

                if (usenumbered && method == Method.ByHierarchy)
                {
                    for (int z = 0; z < SelectedGameObjectObjects.Length; z++)
                    {
                        if ((UnityEngine.Object)SelectedGameObjectObjects[z] == (UnityEngine.Object)SelectedObjects[i])
                        {
                            str = str + ((basenumbered + (stepnumbered * SelectedGameObjectObjects[z].transform.GetSiblingIndex())).ToString());
                        }
                    }
                }

                PreviewSelectedObjects[i] = str;
            }

        }

        private void Rename()
        {
            for (int i = 0; i < SelectedObjects.Length; i++)
            {
                Undo.RecordObject(SelectedObjects[i], "Rename");
                if (usebasename) { SelectedObjects[i].name = basename; }
                if (useprefix) { SelectedObjects[i].name = prefix + SelectedObjects[i].name; }
                if (usesuffix) { SelectedObjects[i].name = SelectedObjects[i].name + suffix; }

                if (usenumbered && method == Method.BySelection) { SelectedObjects[i].name = SelectedObjects[i].name + ((basenumbered + (stepnumbered * i)).ToString()); }

                if (useremove && remove != "") { SelectedObjects[i].name = SelectedObjects[i].name.Replace(remove, ""); }
                if (usereplace && replace != "") { SelectedObjects[i].name = SelectedObjects[i].name.Replace(replace, replacewith); }

                if (AssetDatabase.GetAssetPath(SelectedObjects[i]) != null)
                {
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(SelectedObjects[i]), SelectedObjects[i].name);
                }

            }

            for (int i = 0; i < SelectedGameObjectObjects.Length; i++)
            {
                if (usenumbered && method == Method.ByHierarchy) { SelectedGameObjectObjects[i].name = SelectedGameObjectObjects[i].name + ((basenumbered + (stepnumbered * SelectedGameObjectObjects[i].transform.GetSiblingIndex())).ToString()); }

            }
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
            stepnumbered = 1;

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