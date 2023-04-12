#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastPlacerSceneData : MonoBehaviour
    {
        [Serializable]
        public class Group
        {
            public string name = "";
            public Transform parent = null;
            public List<Element> elements = new();
            public bool isOpen = true;
        }

        [Serializable]
        public class Element
        {
            public GameObject go = null;
            public bool isSelected = false;
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        public List<Group> groups = new();

        public LayerMask RayMask = ~0;

        public Vector3 offset;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public Space space = Space.World;

        public Vector3 startingRot = Vector3.zero;
        public float angleTab = 90f;

        public bool randomizeRotation = false;
        public bool randomizeScale = false;

        public bool chooseRandom = false;

        public float minScale = 0.5f;
        public float maxScale = 1f;

        public Axis currentAxis = Axis.Y;
        public float minAngle = 0f;
        public float maxAngle = 360f;

        public bool useNormals = false;

        public bool showPreview = false;

        public enum PaintMode
        {
            Single,
            Multi
        }

        public PaintMode paintMode = PaintMode.Single;
        public float brushSize = 0.25f;
        public float BrushSize => paintMode == PaintMode.Single ? 0.25f : brushSize;
        public float density = 1f;
        public float minDistance = 0f;

        #region Snapping

        public bool snapX = false;
        public bool snapY = false;
        public bool snapZ = false;
        public bool autoSetX = false;
        public bool autoSetY = false;
        public bool autoSetZ = false;

        public float autoXPos = 0;
        public float autoYPos = 0;
        public float autoZPos = 0;

        #endregion

        public int gridSize = 10;
        public bool drawXY = false;
        public bool drawXZ = false;
        public bool drawYZ = false;

        public Element selected = null;
        public Group selectedGroup = null;

        private void Awake()
        {
            if (!Application.isEditor) Destroy(gameObject);
        }

        #region Selection

        public void Select(Element element)
        {
            if (selected == element) return;

            Deselect();

            selected = element;
            selected.isSelected = true;

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].elements.Contains(element))
                {
                    selectedGroup = groups[i];
                    break;
                }
            }
        }

        public void Deselect()
        {
            if (selected != null)
            {
                selected.isSelected = false;
                selected = null;
                selectedGroup = null;
            }
        }

        #endregion

        #region Grid

        private void OnDrawGizmos()
        {
            DrawGrid(gridSize, 1);
        }

        private void DrawGrid(int gridSize, float unitSize)
        {
            float gridByUnit = gridSize * unitSize;

            Vector3 gridPos = Vector3.zero;
            Vector3 gridUp = Vector3.up;
            Vector3 gridRight = Vector3.right;
            Vector3 gridForward = Vector3.forward;

            Color oldCol = Gizmos.color;
            Gizmos.color = new Color(oldCol.r, oldCol.g, oldCol.b, .2f);

            if (drawXY)
            {
                Vector3 vertPos = gridPos + gridUp * gridByUnit;
                Vector3 negVertPos = gridPos + gridUp * -gridByUnit;
                Vector3 horzPos = gridPos + gridRight * gridByUnit;
                Vector3 negHorzPos = gridPos + gridRight * -gridByUnit;
                // xy plane
                for (int i = 0; i <= gridSize; i++)
                {
                    if (i == 0)
                    {
                        Gizmos.color = new Color(0, 1, 0, .3f);
                        Gizmos.DrawLine(negVertPos, vertPos);
                        Gizmos.color = new Color(1, 0, 0, .3f);
                        Gizmos.DrawLine(negHorzPos, horzPos);
                        Gizmos.color = new Color(oldCol.r, oldCol.g, oldCol.b, .2f);
                    }
                    else
                    {
                        //xy plane
                        // vert lines
                        Gizmos.DrawLine(negVertPos + gridRight * i * unitSize, vertPos + gridRight * i * unitSize);
                        Gizmos.DrawLine(negVertPos + gridRight * -i * unitSize, vertPos + gridRight * -i * unitSize);
                        // horz lines
                        Gizmos.DrawLine(negHorzPos + gridUp * i * unitSize, horzPos + gridUp * i * unitSize);
                        Gizmos.DrawLine(negHorzPos + gridUp * -i * unitSize, horzPos + gridUp * -i * unitSize);
                    }
                }
            }

            if (drawXZ)
            {
                Vector3 forPos = gridPos + gridForward * gridByUnit;
                Vector3 negForPos = gridPos + gridForward * -gridByUnit;
                Vector3 horzPos = gridPos + gridRight * gridByUnit;
                Vector3 negHorzPos = gridPos + gridRight * -gridByUnit;
                for (int i = 0; i <= gridSize; i++)
                {
                    if (i == 0)
                    {
                        // xz
                        Gizmos.color = new Color(0, 0, 1, .3f);
                        Gizmos.DrawLine(negForPos, forPos);
                        Gizmos.color = new Color(1, 0, 0, .3f);
                        Gizmos.DrawLine(negHorzPos, horzPos);
                        Gizmos.color = new Color(oldCol.r, oldCol.g, oldCol.b, .2f);
                    }
                    else
                    {
                        //xz plane
                        Gizmos.DrawLine(negForPos + gridRight * i * unitSize, forPos + gridRight * i * unitSize);
                        Gizmos.DrawLine(negForPos + gridRight * -i * unitSize, forPos + gridRight * -i * unitSize);

                        Gizmos.DrawLine(negHorzPos + gridForward * i * unitSize, horzPos + gridForward * i * unitSize);
                        Gizmos.DrawLine(negHorzPos + gridForward * -i * unitSize, horzPos + gridForward * -i * unitSize);
                    }
                }
            }

            if (drawYZ)
            {
                Vector3 forPos = gridPos + gridForward * gridByUnit;
                Vector3 negForPos = gridPos + gridForward * -gridByUnit;
                Vector3 vertPos = gridPos + gridUp * gridByUnit;
                Vector3 negVertPos = gridPos + gridUp * -gridByUnit;
                // yz plane
                for (int i = 0; i <= gridSize; i++)
                {
                    if (i == 0)
                    {
                        // yz
                        Gizmos.color = new Color(0, 0, 1, .3f);
                        Gizmos.DrawLine(negForPos, forPos);
                        Gizmos.color = new Color(0, 1, 0, .3f);
                        Gizmos.DrawLine(negVertPos, vertPos);
                        Gizmos.color = new Color(oldCol.r, oldCol.g, oldCol.b, .2f);

                    }
                    else
                    {
                        //xz plane
                        Gizmos.DrawLine(negForPos + gridUp * i * unitSize, forPos + gridUp * i * unitSize);
                        Gizmos.DrawLine(negForPos + gridUp * -i * unitSize, forPos + gridUp * -i * unitSize);

                        Gizmos.DrawLine(negVertPos + gridForward * i * unitSize, vertPos + gridForward * i * unitSize);
                        Gizmos.DrawLine(negVertPos + gridForward * -i * unitSize, vertPos + gridForward * -i * unitSize);
                    }
                }
            }

            Gizmos.color = oldCol;
        }

        #endregion
    }
}
#endif