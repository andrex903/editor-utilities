#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class EditorUtilityGUI
    {
        #region ReorderableList

        private static Dictionary<string, ReorderableList> cachedLists;

        public static ReorderableList GetOrCreateCachedList(SerializedProperty property, params string[] propertyNames)
        {
            cachedLists ??= new();

            if (!cachedLists.ContainsKey(property.propertyPath))
            {
                cachedLists[property.propertyPath] = CreateList(property.serializedObject, property, propertyNames);
            }
            return cachedLists[property.propertyPath];
        }

        public static void ClearCachedLists()
        {
            if (cachedLists != null) cachedLists.Clear();
        }

        public static ReorderableList CreateList(SerializedObject serializedObject, SerializedProperty property, params string[] propertyNames)
        {
            ReorderableList list = new(serializedObject, property, true, true, true, true);
            AddCallbacks(list, property.name, propertyNames);
            return list;
        }

        public static ReorderableList CreateList(SerializedObject serializedObject, string listName, params string[] propertyNames)
        {
            return CreateList(serializedObject, serializedObject.FindProperty(listName), propertyNames);
        }

        private static void AddCallbacks(ReorderableList list, string listName, params string[] propertyNames)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, ObjectNames.NicifyVariableName(listName), EditorStyles.boldLabel);
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                float xOffset = 0f;
                if (propertyNames.Length > 0)
                {
                    float width = 1f / propertyNames.Length;
                    for (int i = 0; i < propertyNames.Length; i++)
                    {
                        xOffset += DrawElementGUI(rect, element.FindPropertyRelative(propertyNames[i]), xOffset, width);
                    }
                }
                else DrawElementGUI(rect, element, 0f, 1f);

            };

            list.onCanRemoveCallback = (ReorderableList l) =>
            {
                return l.count > 0;
            };
        }

        private static float DrawElementGUI(Rect rect, SerializedProperty property, float xOffset, float scale)
        {
            Rect propertyRect = new(rect.x + xOffset * rect.width, rect.y + EditorGUIUtility.standardVerticalSpacing, rect.width * scale - 5f, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
            return scale;
        }

        #endregion

        public static void DropAreaGUI(Rect dropArea, Action<UnityEngine.Object> callback)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            callback?.Invoke(draggedObject);
                        }
                    }
                    break;
            }
        }
    }
}
#endif