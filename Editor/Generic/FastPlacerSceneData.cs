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

        public enum PaintMode
        {
            Single,
            Multi
        }

        public float BrushSize
        {
            get
            {
                return paintMode == PaintMode.Single ? MIN_BRUSH_SIZE : brushSize;
            }
            set
            {
                brushSize = Mathf.Clamp(value, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
            }
        }

        public List<Group> groups = new();

        #region Placing Options

        public bool randomizeSelection = false;
        public LayerMask RayMask = ~0;
        public bool alignWithNormals = false;
        public bool showPreview = false;
        public bool showPreviewGizmos = false;
        public PaintMode paintMode = PaintMode.Single;
        public float brushSize = 0.25f;
        public float density = 1f;
        public float minDistance = 0f;
        public bool useColliders = false;
        public LayerMask collidersMask = ~0;

        public const float MIN_BRUSH_SIZE = 0.25f;
        public const float MAX_BRUSH_SIZE = 5f;

        #endregion

        #region Transform

        public Vector3 positionOffset;
        public Space space = Space.World;

        public Axis currentAxis = Axis.Y;
        public float rotationDelta = 90f;
        public bool randomizeRotation = false;
        public Vector3 rotation;
        public float minAngle = 0f;
        public float maxAngle = 360f;

        public bool randomizeScale = false;
        public Vector3 scale = Vector3.one;
        public float minScale = 0.5f;
        public float maxScale = 1f;

        #endregion

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

        #region Grid

        public bool drawXY = false;
        public bool drawXZ = false;
        public bool drawYZ = false;
        public int gridSize = 10;

        #endregion

        public Element selected = null;
        public Group selectedGroup = null;
        public readonly Collider[] colliders = new Collider[1];

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

            foreach (Group group in groups)
            {
                if (group.elements.Contains(element))
                {
                    selectedGroup = group;
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