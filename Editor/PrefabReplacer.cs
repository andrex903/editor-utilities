#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RedeevEditor.Utilities
{
	public class PrefabReplacer : EditorWindow
	{
		[SerializeField] private GameObject prefab;
		[SerializeField] private int value = 100;

		[MenuItem("Tools/Utilities/Prefab Replacer")]
		private static void ShowWindow()
		{
			GetWindow<PrefabReplacer>("Prefab Replacer");
		}

		private void OnGUI()
		{
			prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
			if (GUILayout.Button("Replace"))
			{
				var selection = Selection.gameObjects;
				for (var i = selection.Length - 1; i >= 0; --i)
				{
					var selected = selection[i];
					var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
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

					Undo.RegisterCreatedObjectUndo(instance, "Replace with Prefab");
					instance.transform.parent = selected.transform.parent;
					instance.transform.localPosition = selected.transform.localPosition;
					instance.transform.localRotation = selected.transform.localRotation;
					instance.transform.localScale = selected.transform.localScale;
					instance.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
					Undo.DestroyObjectImmediate(selected);
				}
			}
			value = EditorGUILayout.IntSlider("Percentual", value, 0, 100);
			if (GUILayout.Button("Deselect Random"))
			{
				List<Object> list = new();
				list.AddRange(Selection.objects);
				int deselezionati = Mathf.RoundToInt(list.Count * (value / 100f));
				for (int i = deselezionati - 1; i >= 0; i--)
				{
					list.RemoveAt(Random.Range(0, list.Count));
				}
				Selection.objects = list.ToArray();
			}

			GUI.enabled = false;
			EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
		}
	}
}
#endif
