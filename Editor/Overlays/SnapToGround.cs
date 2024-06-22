#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [Overlay(typeof(SceneView), overlayID, "Snap to ground")]
    [Icon("d_DefaultSorting")]
    public class SnapToGround : IMGUIOverlay
    {
        private int mask = ~0;
        private Direction direction = Direction.Down;
        private float offset = 0f;
        public const string overlayID = "SnapToGround";

        private enum Direction
        {
            All,
            Down,
            Up,
            Right,
            Left,
            Forward,
            Backward
        }

        public override void OnGUI()
        {
            offset = EditorGUILayout.FloatField("Offset", offset);
            LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask), InternalEditorUtility.layers);
            mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            direction = (Direction)EditorGUILayout.EnumPopup(direction);

            var selection = Selection.transforms;
            int count = selection.Length;
            if (count == 0) GUI.enabled = false;
            string text = count == 1 ? $"Snap ({selection[0].name})" : $"Snap ({count} elements)";
            if (GUILayout.Button(text, GUILayout.Height(25f))) SetPostion(selection);
            GUI.enabled = true;
        }

        private void SetPostion(Transform[] selection)
        {
            Undo.SetCurrentGroupName("GameObjects snapped");
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(selection, "transform selected objects");

            foreach (var selected in selection)
            {
                SetPosition(selected);
            }

            Undo.CollapseUndoOperations(group);
        }

        private void SetPosition(Transform selected)
        {
            float distance = float.MaxValue;
            Vector3 hitPoint = selected.position;
            Vector3 resultDirection = Vector3.zero;

            switch (direction)
            {
                case Direction.Down:
                    TestDirection(Vector3.down, false);
                    break;
                case Direction.Up:
                    TestDirection(Vector3.up, false);
                    break;
                case Direction.Right:
                    TestDirection(Vector3.right, false);
                    break;
                case Direction.Left:
                    TestDirection(Vector3.left, false);
                    break;
                case Direction.Forward:
                    TestDirection(Vector3.forward, false);
                    break;
                case Direction.Backward:
                    TestDirection(Vector3.back, false);
                    break;
                case Direction.All:
                    TestDirection(Vector3.forward);
                    TestDirection(Vector3.back);
                    TestDirection(Vector3.left);
                    TestDirection(Vector3.right);
                    TestDirection(Vector3.up);
                    TestDirection(Vector3.down);
                    break;
            }

            selected.position = hitPoint + resultDirection * offset;

            void TestDirection(Vector3 direction, bool checkDistance = true)
            {
                if (Physics.Raycast(selected.position, direction, out RaycastHit hit, 1000f, mask))
                {
                    if (checkDistance)
                    {
                        float currentDistance = Vector3.Distance(selected.position, hit.point);
                        if (currentDistance < distance)
                        {
                            distance = currentDistance;
                            hitPoint = hit.point;
                            resultDirection = -direction;
                        }
                    }
                    else
                    {
                        hitPoint = hit.point;
                        resultDirection = -direction;
                    }
                }
            }
        }
    }
}
#endif