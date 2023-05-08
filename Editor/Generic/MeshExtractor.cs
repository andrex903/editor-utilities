#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class MeshExtractor
    {
        private static readonly string TITLE = "Extracting Meshes";
        private static readonly string SOURCE_EXT = ".fbx";
        private static readonly string TARGET_EXT = ".asset";

        [MenuItem("Assets/Redeev/Extract Meshes", validate = true)]
        private static bool ExtractMeshesMenuItemValidate()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                if (!AssetDatabase.GetAssetPath(Selection.objects[i]).EndsWith(SOURCE_EXT)) return false;
            }
            return true;
        }

        [MenuItem("Assets/Redeev/Extract Meshes")]
        private static void ExtractMeshesMenuItem()
        {
            EditorUtility.DisplayProgressBar(TITLE, "", 0);
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                EditorUtility.DisplayProgressBar(TITLE, Selection.objects[i].name, (float)i / (Selection.objects.Length - 1));
                ExtractMeshes(Selection.objects[i]);
            }
            EditorUtility.ClearProgressBar();
        }

        private static void ExtractMeshes(Object selectedObject)
        {        
            //Create Folders
            string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);
            string parentfolderPath = selectedObjectPath.Substring(0, selectedObjectPath.Length - (selectedObject.name.Length + 5));
            string objectFolderName = selectedObject.name;
            string objectFolderPath = parentfolderPath + "/" + objectFolderName;
            string meshFolderName = "Meshes";
            string meshFolderPath = objectFolderPath + "/" + meshFolderName;

            if (!AssetDatabase.IsValidFolder(objectFolderPath))
            {
                AssetDatabase.CreateFolder(parentfolderPath, objectFolderName);
                if (!AssetDatabase.IsValidFolder(meshFolderPath)) AssetDatabase.CreateFolder(objectFolderPath, meshFolderName);             
            }

            //Create Meshes
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(selectedObjectPath);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is Mesh)
                {
                    EditorUtility.DisplayProgressBar(TITLE, selectedObject.name + " : " + objects[i].name, (float)i / (objects.Length - 1));

                    Mesh mesh = Object.Instantiate(objects[i]) as Mesh;

                    AssetDatabase.CreateAsset(mesh, meshFolderPath + "/" + objects[i].name + TARGET_EXT);
                }
            }

            //Cleanup
            AssetDatabase.MoveAsset(selectedObjectPath, objectFolderPath + "/" + selectedObject.name + SOURCE_EXT);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif