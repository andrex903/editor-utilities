#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class EditorUtilityGUI
    {
        #region ReorderableList

        public static ReorderableList CreateList(SerializedObject serializedObject, string listName, params string[] elements)
        {
            ReorderableList list = new(serializedObject, serializedObject.FindProperty(listName), true, true, true, true);
            AddCallbacks(list, listName, elements);
            return list;
        }

        private static void AddCallbacks(ReorderableList list, string listName, params string[] elements)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, ObjectNames.NicifyVariableName(listName), EditorStyles.boldLabel);
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                float offset = 0f;

                if (elements.Length > 0)
                {
                    for (int i = 0; i < elements.Length; i++)
                    {
                        offset += AddElement(rect, element.FindPropertyRelative(elements[i]), offset, 10f / elements.Length);
                    }
                }
                else AddElement(rect, element, 0f, 10f);

            };

            list.onCanRemoveCallback = (ReorderableList l) =>
            {
                return l.count > 0;
            };
        }

        private static float AddElement(Rect rect, SerializedProperty property, float x, float width)
        {
            EditorGUI.PropertyField(new Rect(rect.x + x / 10 * rect.width, rect.y, rect.width * width / 10f - 5f, EditorGUIUtility.singleLineHeight), property, GUIContent.none);
            return width;
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