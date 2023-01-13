#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastAlign : EditorWindow
    {
        private enum Axis
        {
            X,
            Z
        }
        private Axis axis = Axis.X;       
        private int number = 1;
        private Vector2 distance = Vector2.one;

        private int selectionCount = 0;
        private int lastSelectionCount = -1;

        [MenuItem("Tools/Utilities/Fast Align")]
        public static void ShowWindow()
        {
            GetWindow<FastAlign>("Fast Align");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            axis = (Axis)EditorGUILayout.EnumPopup("Direction", axis);
            number = EditorGUILayout.IntField("Rows", number);
            distance = EditorGUILayout.Vector2Field("Spacing", distance);
            GUI.enabled = selectionCount > 1;
            if (GUILayout.Button("Align")) Align();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            selectionCount = Selection.count;
            if (selectionCount != lastSelectionCount)
            {
                lastSelectionCount = selectionCount;
                Repaint();
            }
        }


        public void Align()
        {
            List<GameObject> list = new(Selection.gameObjects);
            List<GameObject> remove = new();
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].transform.childCount; j++)
                {
                    remove.Add(list[i].transform.GetChild(j).gameObject);
                }
            }
            for (int i = 0; i < remove.Count; i++)
            {
                list.Remove(remove[i]);
            }

            if (list.Count == 0) return;

            List<int> elements;

            elements = Align(list.Count, number);

            int index = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                for (int j = 0; j < elements[i]; j++)
                {
                    if (axis == Axis.Z) list[index].transform.localPosition = new Vector3(i * distance.x, 0f, j * distance.y);
                    else list[index].transform.localPosition = new Vector3(j * distance.x, 0f, i * distance.y);
                    index++;
                }
            }

            //Vector3 positon = transform.position;
            //CenterOnChildred(transform);
            //transform.position = positon;
        }

        private List<int> Align(int total, int fixedNumber)
        {
            List<int> elements = new();

            int left = total / fixedNumber;

            for (int i = 0; i < fixedNumber; i++)
            {
                int e = Mathf.Min(total, left);
                elements.Add(e);
                total -= e;
                if (total <= 0) break;
            }

            if (total > 0)
            {
                int index = 0;
                while (total > 0)
                {
                    elements[index] += 1;
                    total--;
                    index++;
                    index %= elements.Count;
                }
            }
            return elements;
        }

        private void CenterOnChildred(Transform parent)
        {
            var childs = parent.Cast<Transform>().ToList();
            var pos = Vector3.zero;
            foreach (Transform child in childs)
            {
                pos += child.position;
                child.parent = null;
            }
            pos /= childs.Count;
            parent.position = pos;
            foreach (var child in childs) child.parent = parent;
        }
    }
}
#endif