#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastAlign : EditorWindow
    {
        private enum Mode
        {
            Pivot,
            Center,
            Border
        }

        public enum BoundsSource
        {
            AllChildren,
            FirstChild,
            CustomChild
        }

        public enum CenterType
        {
            LocalZero,
            WorldZero
        }

        private enum Axis
        {
            X,
            Z
        }

        private Mode mode = Mode.Pivot;
        private BoundsSource source = BoundsSource.AllChildren;
        private string childName = string.Empty;
        private Axis axis = Axis.X;
        private CenterType center = CenterType.LocalZero;
        private int rows = 1;
        private float spacingX = 1f;
        private float spacingZ = 1f;

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
            mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);
            if (mode != Mode.Pivot)
            {
                source = (BoundsSource)EditorGUILayout.EnumPopup("Source", source);
                if (source == BoundsSource.CustomChild) childName = EditorGUILayout.TextField("Child Name", childName);
            }
            axis = (Axis)EditorGUILayout.EnumPopup("Direction", axis);
            if (mode != Mode.Border)
            {
                rows = Mathf.Max(1, EditorGUILayout.IntField("Max Lenght", rows));
                center = (CenterType)EditorGUILayout.EnumPopup("Center", center);
            }
            float label = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 15f;
            EditorGUILayout.BeginHorizontal();
            spacingX = EditorGUILayout.FloatField("X", spacingX);
            spacingZ = EditorGUILayout.FloatField("Z", spacingZ);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = label;
            GUI.enabled = selectionCount > 1;
            EditorGUILayout.Space(5);
            if (GUILayout.Button($"Align ({selectionCount})", GUILayout.Height(30f))) Align();
            EditorGUILayout.EndVertical();
        }

        private void OnInspectorUpdate()
        {
            selectionCount = GetSelectionWithoutChildren().Count;
            if (selectionCount != lastSelectionCount)
            {
                lastSelectionCount = selectionCount;
                Repaint();
            }
        }

        public void Align()
        {
            List<GameObject> gameObjects = GetSelectionWithoutChildren();
            if (gameObjects.Count == 0) return;

            Undo.SetCurrentGroupName("Align selected objects");
            int group = Undo.GetCurrentGroup();

            Transform[] t = new Transform[gameObjects.Count];
            for (int i = 0; i < gameObjects.Count; i++)
            {
                t[i] = gameObjects[i].transform;
            }
            Undo.RecordObjects(t, "Align selected objects");

            Vector3 center = CalculateCenter(gameObjects);
            Vector3 offset = GetPosition((gameObjects.Count - 1) / rows, Mathf.Min(rows, gameObjects.Count) - 1);

            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (mode == Mode.Border)
                {
                    if (i == 0)
                    {
                        Apply(i, Vector3.zero);
                    }
                    else
                    {
                        Vector3 offset2;
                        if (axis == Axis.Z) offset2 = new Vector3(0, 0, spacingZ + GetBounds(gameObjects[i]).extents.z + GetBounds(gameObjects[i - 1]).extents.z);
                        else offset2 = new Vector3(spacingX + GetBounds(gameObjects[i]).extents.x + GetBounds(gameObjects[i - 1]).extents.x, 0, 0);
                        Apply(i, GetBoundCenter(gameObjects[i - 1]) + offset2);
                    }
                }
                else Apply(i, GetPosition(i / rows, i % rows) - offset / 2f + center);
            }

            Undo.CollapseUndoOperations(group);

            void Apply(int goIndex, Vector3 position)
            {
                if (mode == Mode.Center || mode == Mode.Border)
                {
                    Vector3 realCenter = GetBoundCenter(gameObjects[goIndex]);
                    position += (gameObjects[goIndex].transform.position - realCenter);
                }

                if (this.center == CenterType.LocalZero) gameObjects[goIndex].transform.localPosition = position;
                else gameObjects[goIndex].transform.position = position;
            }
        }

        private Vector3 GetPosition(int i, int j)
        {
            return axis switch
            {
                Axis.X => new Vector3(j * spacingX, 0f, i * spacingZ),
                Axis.Z => new Vector3(i * spacingX, 0f, j * spacingZ),
                _ => Vector3.zero,
            };
        }

        private Vector3 CalculateCenter(List<GameObject> gos)
        {
            switch (center)
            {
                //case CenterType.Barycenter:
                //    Vector3 sumVector = Vector3.zero;
                //    foreach (GameObject go in gos) sumVector += go.transform.position;
                //    return sumVector / gos.Count;
                //case CenterType.First:
                //    return gos[0].transform.position;
                case CenterType.WorldZero:
                    return Vector3.zero;
                case CenterType.LocalZero:
                    return Vector3.zero;
            }
            return Vector3.zero;
        }

        private List<GameObject> GetSelectionWithoutChildren()
        {
            if (Selection.objects.Length == 0) return new();

            List<GameObject> gameObjects = new();
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject go && go.scene.IsValid()) gameObjects.Add(go);
                else return new();
            }
            if (gameObjects.Count == 0) return new();

            List<GameObject> childs = new();
            for (int i = 0; i < gameObjects.Count; i++)
            {
                for (int j = 0; j < gameObjects[i].transform.childCount; j++)
                {
                    childs.Add(gameObjects[i].transform.GetChild(j).gameObject);
                }
            }
            for (int i = 0; i < childs.Count; i++)
            {
                gameObjects.Remove(childs[i]);
            }

            return gameObjects;
        }

        #region Bounds

        private Vector3 GetBoundCenter(GameObject obj)
        {
            Vector3 center = GetBounds(obj).center;
            center.y = 0;
            return center;
        }

        private Bounds GetBounds(GameObject room)
        {
            Bounds bounds = new(room.transform.position, Vector3.zero);

            switch (source)
            {
                case BoundsSource.FirstChild:
                    if (room.transform.childCount > 0) bounds = GetRenderBounds(room.transform.GetChild(0).gameObject);
                    break;
                case BoundsSource.CustomChild:
                    if (!string.IsNullOrEmpty(childName))
                    {
                        foreach (Transform child in room.transform)
                        {
                            if (child.gameObject.name.Equals(childName) && child.TryGetComponent(out Renderer childRender))
                            {
                                bounds = childRender.bounds;
                                break;
                            }
                        }
                    }
                    break;
                case BoundsSource.AllChildren:
                    bounds = GetRenderBounds(room);
                    foreach (Transform child in room.transform)
                    {
                        if (child.TryGetComponent(out Renderer childRender)) bounds.Encapsulate(childRender.bounds);
                        else bounds.Encapsulate(GetBounds(child.gameObject));
                    }
                    break;
            }

            return bounds;
        }

        private Bounds GetRenderBounds(GameObject element)
        {
            Bounds bounds = new(element.transform.position, Vector3.zero);
            if (element.TryGetComponent(out Renderer render)) return render.bounds;
            return bounds;
        }

        #endregion     
    }
}
#endif