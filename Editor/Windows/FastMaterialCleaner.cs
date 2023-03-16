using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastMaterialCleaner : EditorWindow
    {
        private enum PropertyType
        {
            TexEnv,
            Int,
            Float,
            Color
        }

        private List<Material> selectedMaterials = new();
        private SerializedObject[] serializedObjects;
        private Vector2 scrollPos;
        private GUIStyle warningStyle, errorStyle;

        private const float REMOVE_BUTTON_WIDTH = 60f;
        private const float TYPE_SPACING = 4f;
        private const float SCROLLBAR_WIDTH = 15f;

        [MenuItem("Tools/Utilities/Fast Material Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<FastMaterialCleaner>("Fast Material Cleaner");
        }

        protected virtual void OnEnable()
        {
            GetSelectedMaterials();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            Repaint();
        }

        protected virtual void OnSelectionChange()
        {
            GetSelectedMaterials();
        }

        protected virtual void OnProjectChange()
        {
            GetSelectedMaterials();
        }

        private void CleanMaterial(int index)
        {
            Material material = selectedMaterials[index];
            if (HasShader(material))
            {
                RemoveUnusedProperties("m_SavedProperties.m_TexEnvs", index, PropertyType.TexEnv);
                RemoveUnusedProperties("m_SavedProperties.m_Ints", index, PropertyType.Int);
                RemoveUnusedProperties("m_SavedProperties.m_Floats", index, PropertyType.Float);
                RemoveUnusedProperties("m_SavedProperties.m_Colors", index, PropertyType.Color);
            }
            else Debug.LogError("Material " + material.name + " doesn't have a shader");
        }

        protected virtual void OnGUI()
        {
            if (selectedMaterials == null || selectedMaterials.Count <= 0)
            {
                EditorGUILayout.LabelField("No Materials Selected", EditorStyles.largeLabel);
            }
            else
            {
                EditorGUIUtility.labelWidth = position.width * 0.5f - SCROLLBAR_WIDTH - 2;
                GUIStyle typeLabelStyle = new("LargeLabel");
                errorStyle = new GUIStyle("CN StatusError");
                warningStyle = new GUIStyle("CN StatusWarn");

                EditorGUILayout.Space(TYPE_SPACING);

                if (GUILayout.Button($"Clean All Materials ({selectedMaterials.Count})"))
                {
                    for (int i = 0; i < selectedMaterials.Count; i++) CleanMaterial(i);       
                    GUIUtility.ExitGUI();
                }
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                EditorGUILayout.BeginVertical();

                for (int i = 0; i < selectedMaterials.Count; i++)
                {
                    Material m_selectedMaterial = selectedMaterials[i];

                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField(m_selectedMaterial.name + ":", EditorStyles.whiteLargeLabel);

                    serializedObjects[i].Update();

                    EditorGUI.indentLevel++;
                    {
                        EditorGUILayout.Space(TYPE_SPACING);

                        EditorGUILayout.LabelField("Textures:", typeLabelStyle);
                        EditorGUI.indentLevel++;
                        ProcessProperties("m_SavedProperties.m_TexEnvs", i, PropertyType.TexEnv);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(TYPE_SPACING);

                        EditorGUILayout.LabelField("Ints:", typeLabelStyle);
                        EditorGUI.indentLevel++;
                        ProcessProperties("m_SavedProperties.m_Ints", i, PropertyType.Int);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(TYPE_SPACING);

                        EditorGUILayout.LabelField("Floats:", typeLabelStyle);
                        EditorGUI.indentLevel++;
                        ProcessProperties("m_SavedProperties.m_Floats", i, PropertyType.Float);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(TYPE_SPACING);

                        EditorGUILayout.LabelField("Colors:", typeLabelStyle);
                        EditorGUI.indentLevel++;
                        ProcessProperties("m_SavedProperties.m_Colors", i, PropertyType.Color);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space(TYPE_SPACING);

                    if (GUILayout.Button("Clean Material"))
                    {
                        CleanMaterial(i);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();

                EditorGUIUtility.labelWidth = 0;
            }
        }

        private static bool ShaderHasProperty(Material material, string name, PropertyType type)
        {
            return type switch
            {
                PropertyType.TexEnv => material.HasTexture(name),
                PropertyType.Int => material.HasInteger(name),
                PropertyType.Float => material.HasFloat(name),
                PropertyType.Color => material.HasColor(name),
                _ => false,
            };
        }

        private static string GetName(SerializedProperty property)
        {
            return property.FindPropertyRelative("first").stringValue;
        }

        private static bool HasShader(Material mat)
        {
            return mat.shader.name != "Hidden/InternalErrorShader";
        }

        private void RemoveUnusedProperties(string path, int index, PropertyType type)
        {
            if (!HasShader(selectedMaterials[index]))
            {
                Debug.LogError("Material " + selectedMaterials[index].name + " doesn't have a shader");
                return;
            }

            var properties = serializedObjects[index].FindProperty(path);
            if (properties != null && properties.isArray)
            {
                for (int j = properties.arraySize - 1; j >= 0; j--)
                {
                    string propName = GetName(properties.GetArrayElementAtIndex(j));
                    bool exists = ShaderHasProperty(selectedMaterials[index], propName, type);

                    if (!exists)
                    {
                        Debug.Log("Removed " + type + " Property: " + propName);
                        properties.DeleteArrayElementAtIndex(j);
                        serializedObjects[index].ApplyModifiedProperties();
                    }
                }
            }
        }

        private void ProcessProperties(string path, int index, PropertyType type)
        {
            var properties = serializedObjects[index].FindProperty(path);
            if (properties != null && properties.isArray)
            {
                for (int j = 0; j < properties.arraySize; j++)
                {
                    string propName = GetName(properties.GetArrayElementAtIndex(j));
                    bool exists = ShaderHasProperty(selectedMaterials[index], propName, type);

                    if (!HasShader(selectedMaterials[index]))
                    {
                        EditorGUILayout.LabelField(propName, "UNKNOWN", errorStyle);
                    }
                    else if (exists)
                    {
                        EditorGUILayout.LabelField(propName, "Exists");
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        float witdth = EditorGUIUtility.labelWidth * 2 - REMOVE_BUTTON_WIDTH;
                        EditorGUILayout.LabelField(propName, "Old Reference", warningStyle, GUILayout.Width(witdth));
                        if (GUILayout.Button("Remove", GUILayout.Width(REMOVE_BUTTON_WIDTH)))
                        {
                            properties.DeleteArrayElementAtIndex(j);
                            serializedObjects[index].ApplyModifiedProperties();
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void GetSelectedMaterials()
        {
            Object[] objects = Selection.objects;

            selectedMaterials = new List<Material>();

            for (int i = 0; i < objects.Length; i++)
            {
                Material newMat = objects[i] as Material;
                if (newMat != null) selectedMaterials.Add(newMat);
            }

            if (selectedMaterials != null)
            {
                serializedObjects = new SerializedObject[selectedMaterials.Count];
                for (int i = 0; i < serializedObjects.Length; i++) serializedObjects[i] = new SerializedObject(selectedMaterials[i]);
            }

            Repaint();
        }
    }
}