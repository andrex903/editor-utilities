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
                if (Selection.objects.Length > 0)
                {
                    var selected = Selection.objects[0];
                    if (selected is GameObject g)
                    {
                        RaycastHit[] hits = Physics.RaycastAll(g.transform.position + Vector3.up * 100f, -Vector3.up, 1000f);
                        if (hits.Length > 0) g.transform.position = hits[0].point;
                    }
                }
            }
        }
    }
}
#endif