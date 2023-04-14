#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [Overlay(typeof(SceneView), k_OverlayID, "Snap Overlay")]
    [Icon("d_DefaultSorting")]
    public class SnapOverlay : IMGUIOverlay
    {
        public const string k_OverlayID = "SnapOverlay";

        public override void OnGUI()
        {
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_GridAxisY")))
            {
                for (int i = 0; i < Selection.objects.Length; i++)
                {
                    var selected = Selection.objects[i];
                    if (selected is GameObject g)
                    {
                        RaycastHit[] hits = Physics.RaycastAll(g.transform.position + Vector3.up * 100f, -Vector3.up, 1000f);
                        if (hits.Length > 0)
                        {
                            if (hits[0].transform == g.transform)
                            {
                                if (hits.Length > 1) SetPostion(g.transform, hits[1].point);
                            }
                            else SetPostion(g.transform, hits[0].point);
                        }
                    }
                }
            }
        }

        private void SetPostion(Transform transform, Vector3 position)
        {
            Undo.RecordObject(transform, "Transform snapped");
            transform.position = position;
        }
    }
}
#endif