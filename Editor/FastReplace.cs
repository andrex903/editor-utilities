#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastReplace : EditorWindow
    {
        private GameObject prefab;
        private int value = 50;
        private int selectionCount = 0;
        private int lastSelectionCount = -1;
        private bool useSelection = true;
        [SerializeField] private List<GameObject> objectsToReplace = new();
        private ReorderableList list;
        private SerializedObject so;

        [MenuItem("Tools/Utilities/Fast Replace")]
        private static void ShowWindow()
        {
            GetWindow<FastReplace>("Fast Replace");
        }

        private void OnEnable()
        {
            so = new SerializedObject(this);
            list = EditorUtilityGUI.CreateList(so, nameof(objectsToReplace));               
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            prefab = (GameObject)EditorGUILayout.ObjectField("Replace With", prefab, typeof(GameObject), true);
            useSelection = EditorGUILayout.Toggle("Use Selection", useSelection);
            if (useSelection)
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Active Selections: " + selectionCount);
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                value = EditorGUILayout.IntSlider("Percentual", value, 0, 100);
                if (GUILayout.Button("Deselect Random")) DeselectRandom();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else
            {
                so.Update();
                list.DoLayoutList();
                so.ApplyModifiedProperties();
                
                EditorUtilityGUI.DropAreaGUI(GUILayoutUtility.GetLastRect(), go =>
                {
                    if (go.GetType() == typeof(GameObject)) AddToList(go as GameObject);
                });                         
            }
            if (GUILayout.Button("Replace")) Replace();
            EditorGUILayout.EndVertical();
        }

        private void AddToList(GameObject go)
        {
            if (go && !objectsToReplace.Contains(go)) objectsToReplace.Add(go);
        }

        private void Update()
        {
            if (!useSelection) return;

            selectionCount = Selection.count;
            if (selectionCount != lastSelectionCount)
            {
                lastSelectionCount = selectionCount;
                Repaint();
            }
        }

        private void DeselectRandom()
        {
            List<Object> list = new(Selection.objects);
            int count = Mathf.RoundToInt(list.Count * (value / 100f));
            for (int i = count - 1; i >= 0; i--)
            {
                list.RemoveAt(Random.Range(0, list.Count));
            }
            Selection.objects = list.ToArray();
        }

        private void Replace()
        {
            var selection = useSelection ? Selection.gameObjects : objectsToReplace.ToArray();
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefab);
            for (int i = selection.Length - 1; i >= 0; --i)
            {
                if (selection[i] == null || selection[i] == prefab) continue;

                GameObject instance = null;
                if (prefabType == PrefabAssetType.NotAPrefab)
                {
                    instance = Instantiate(prefab);
                    instance.name = prefab.name;
                }
                else if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }

                if (instance == null)
                {
                    Debug.LogError("Error instantiating prefab");
                    break;
                }

                Undo.RegisterCreatedObjectUndo(instance, $"{selection[i].name} replaced");
                instance.transform.parent = selection[i].transform.parent;
                instance.transform.localPosition = selection[i].transform.localPosition;
                instance.transform.localRotation = selection[i].transform.localRotation;
                instance.transform.localScale = selection[i].transform.localScale;
                instance.transform.SetSiblingIndex(selection[i].transform.GetSiblingIndex());
                Undo.DestroyObjectImmediate(selection[i]);
                if (!useSelection) objectsToReplace.Clear();
            }
        }
    }
}
#endif