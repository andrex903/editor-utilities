using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class AlignOnGrid : MonoBehaviour
    {
        public enum Axis
        {
            X,
            Z
        }
        [SerializeField] Axis axis = Axis.X;
        [Min(1)]
        [SerializeField] int number = 1;
        [SerializeField] Vector2 distance = Vector2.one;

        [ContextMenu("Align")]
        public void Align()
        {
            List<Transform> list = new(GetComponentsInChildren<Transform>());
            List<Transform> remove = new();
            list.Remove(transform);
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].childCount; j++)
                {
                    remove.Add(list[i].GetChild(j));
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
                    if (axis == Axis.X) list[index].localPosition = new Vector3(i * distance.x, 0f, j * distance.y);
                    else list[index].localPosition = new Vector3(j * distance.x, 0f, i * distance.y);
                    index++;
                }
            }

            Vector3 positon = transform.position;
            CenterOnChildred(transform);
            transform.position = positon;
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