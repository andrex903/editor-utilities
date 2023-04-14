#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [Overlay(typeof(SceneView), k_OverlayID, "Snap Overlay")]
    [Icon("d_DefaultSorting")]
    public class SnapOverlay : IMGUIOverlay
    {
        private int mask = 1;
        private float offset = 0f;
        public const string k_OverlayID = "SnapOverlay";

        public override void OnGUI()
        {
            offset = EditorGUILayout.FloatField("Offset", offset);
            LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask), InternalEditorUtility.layers);
            mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_GridAxisY"))) SetPostion(Vector3.up); 
        }

        private void SetPostion(Vector3 direction)
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var selected = Selection.objects[i];
                if (selected is GameObject gameObject)
                {
                    RaycastHit[] hits = Physics.RaycastAll(gameObject.transform.position + direction * 500f, -direction, 1000f);
                    for (int j = 0; j < hits.Length; j++)
                    {
                        if (hits[j].transform == gameObject.transform) continue;
                        if (mask != (mask | (1 << hits[j].transform.gameObject.layer))) continue;

                        Undo.RecordObject(gameObject.transform, "Transform snapped");
                        gameObject.transform.position = hits[j].point + Vector3.up * offset;
                        return;
                    }

                }
            }
        }
    }
}
#endif