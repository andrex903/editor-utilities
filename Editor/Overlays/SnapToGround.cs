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
        private float offset = 0f;
        public const string overlayID = "SnapToGround";

        public override void OnGUI()
        {
            offset = EditorGUILayout.FloatField("Offset", offset);
            LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask), InternalEditorUtility.layers);
            mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            var selection = Selection.transforms;
            int count = selection.Length;
            if (count == 0) GUI.enabled = false;
            string text = count == 1 ? $"Snap ({selection[0].name})" : $"Snap ({count} elements)";
            if (GUILayout.Button(text)) SetPostion(selection, Vector3.up);
            GUI.enabled = true;
        }

        private void SetPostion(Transform[] selection, Vector3 direction)
        {
            Undo.SetCurrentGroupName("GameObjects snapped");
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(selection, "transform selected objects");

            for (int i = 0; i < selection.Length; i++)
            {
                var selected = selection[i];   
                RaycastHit[] hits = Physics.RaycastAll(selected.position + direction * 500f, -direction, 1000f);
                for (int j = 0; j < hits.Length; j++)
                {
                    if (hits[j].transform == selected) continue;
                    if (mask != (mask | (1 << hits[j].transform.gameObject.layer))) continue;

                    selected.position = hits[j].point + Vector3.up * offset;
                    break;
                }
            }

            Undo.CollapseUndoOperations(group);
        }
    }
}
#endif