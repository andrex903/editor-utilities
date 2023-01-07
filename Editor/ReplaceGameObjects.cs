#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RedeevEditor.Utilities
{
	public class ReplaceGameObjects : ScriptableWizard
	{
		public bool useSelection = false;
		public GameObject newObject;
		public GameObject[] OldObjects;

		[MenuItem("Tools/Utilities/Replace GameObjects")]
		static void CreateWizard()
		{
			DisplayWizard("Replace GameObjects", typeof(ReplaceGameObjects), "Replace");
		}

		void OnWizardCreate()
		{
			if (useSelection)
			{
				OldObjects = Selection.gameObjects;
			}
			GameObject newObject;
			foreach (GameObject go in OldObjects)
			{
				newObject = (GameObject)PrefabUtility.InstantiatePrefab(this.newObject);
				newObject.transform.position = go.transform.position;
				newObject.transform.rotation = go.transform.rotation;
				newObject.transform.parent = go.transform.parent;
				DestroyImmediate(go);
			}

		}
	}
}
#endif