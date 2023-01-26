#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static RedeevEditor.Utilities.FastPlacerSceneData;

namespace RedeevEditor.Utilities
{
    public class FastPlacer : EditorWindow
    {
        private Vector3 hitPoint;
        private Vector3 hitNormal;
        private Transform root;

        private int controlID;
        private bool paintingEnabled = false;

        private bool offsetFoldout = false;
        private bool snapFoldout = false;
        private bool gridFoldout = false;
        private bool placingOptionsFoldout = false;

        private readonly int HASH = "FastPlacer".GetHashCode();

        private FastPlacerSceneData sceneData = null;

        private FastPlacerSceneData SceneData
        {
            get
            {
                if (sceneData == null)
                {
                    sceneData = FindObjectOfType<FastPlacerSceneData>();
                    if (sceneData == null)
                    {
                        sceneData = new GameObject(nameof(FastPlacerSceneData)).AddComponent<FastPlacerSceneData>();
                        sceneData.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                }
                return sceneData;
            }
        }

        [MenuItem("Tools/Utilities/Fast Placer")]
        public static void ShowWindow()
        {
            GetWindow<FastPlacer>("Fast Placer");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) paintingEnabled = false;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        #region GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            Event evt = Event.current;
            controlID = GUIUtility.GetControlID(HASH, FocusType.Passive);

            if (!paintingEnabled || evt.alt) return;

            if (evt.Equals(Event.KeyboardEvent("Escape")))
            {
                paintingEnabled = false;
                evt.Use();
                Repaint();
                return;
            }

            if (evt.Equals(Event.KeyboardEvent("Tab")))
            {
                AddRotation();
                evt.Use();
                Repaint();
                return;
            }

            SetTargetPoint(evt);
            SetTargetVisualization();

            if (evt.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
                return;
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                PlaceGameObject();
                evt.Use();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Not available in playMode");
                return;
            }

            if (GUILayout.Button(paintingEnabled ? "Stop" : "Start", GUILayout.Height(30)))
            {
                paintingEnabled = !paintingEnabled;
            }

            EditorGUI.BeginChangeCheck();
            GroupsGUI();
            PlacingOptionsGUI();
            OffsetRoationAndScaleGUI();
            SnappingGUI();
            GridGUI();
            if (GUILayout.Button("Clear Scene Settings"))
            {
                if (EditorUtility.DisplayDialog("Confirmation required", "Are you sure to delete the current scene settings?", "Confirm", "Cancel")) DestroyImmediate(SceneData.gameObject);
            }

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(SceneData);
        }

        private void PlacingOptionsGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            if (placingOptionsFoldout = EditorGUILayout.Foldout(placingOptionsFoldout, "Placing Options"))
            {
                EditorGUI.indentLevel++;
                LayerMask tempMask = EditorGUILayout.MaskField("Raycast Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(SceneData.RayMask), InternalEditorUtility.layers);
                SceneData.RayMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                SceneData.useNormals = EditorGUILayout.Toggle("Align with Normals", SceneData.useNormals);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void GroupsGUI()
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.LabelField("Groups:", EditorStyles.boldLabel);

            for (int i = 0; i < SceneData.groups.Count; i++)
            {
                EditorGUILayout.BeginVertical("Box");
                if (SceneData.groups[i].name.Equals(string.Empty)) SceneData.groups[i].name = "Group" + i;

                if (SceneData.groups[i].isOpen = EditorGUILayout.Foldout(SceneData.groups[i].isOpen, SceneData.groups[i].name, EditorStyles.foldout))
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    SceneData.groups[i].name = EditorGUILayout.TextField(SceneData.groups[i].name);
                    if (SceneData.groups[i].name.Equals(string.Empty)) SceneData.groups[i].name = "Group " + i;

                    if (GUILayout.Button("Remove Group"))
                    {
                        if (SceneData.groups[i] == SceneData.selectedGroup) SceneData.Deselect();
                        SceneData.groups.RemoveAt(i);
                        GUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    GUILayout.EndHorizontal();

                    SceneData.groups[i].parent = EditorGUILayout.ObjectField("Container", SceneData.groups[i].parent, typeof(Transform), true) as Transform;

                    GroupElementsGUI(SceneData.groups[i]);

                    if (GUILayout.Button("+"))
                    {
                        SceneData.groups[i].elements.Add(new Element());
                    }

                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Create Group")) SceneData.groups.Add(new Group());

            EditorGUILayout.EndVertical();
        }

        private void GroupElementsGUI(Group group)
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.LabelField("GameObjects:", EditorStyles.boldLabel);

            group.useRandom = GUILayout.Toggle(group.useRandom, "  Choose Random");
            if (group.useRandom)
            {
                SceneData.Deselect();
                SceneData.selectedGroup = group;
            }

            EditorGUILayout.Space(1);

            foreach (var element in group.elements)
            {
                if (SceneData.selected == element && !element.go)
                {
                    SceneData.Deselect();
                }
                EditorGUILayout.BeginHorizontal();

                bool oldEnabled = GUI.enabled;
                GUI.enabled = element.go && !group.useRandom;
                element.isSelected = GUILayout.Toggle(element.isSelected, "", GUILayout.Width(20));
                GUI.enabled = oldEnabled;
                if (element.isSelected) SceneData.Select(element);

                element.go = EditorGUILayout.ObjectField(element.go, typeof(GameObject), true) as GameObject;

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    group.elements.Remove(element);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void OffsetRoationAndScaleGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            if (offsetFoldout = EditorGUILayout.Foldout(offsetFoldout, "Transform"))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("Box");
                SceneData.offset = EditorGUILayout.Vector3Field("Position Offset", SceneData.offset);
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical("Box");
                SceneData.randomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", SceneData.randomizeRotation);
                if (SceneData.randomizeRotation)
                {
                    SceneData.currentAxis = (Axis)EditorGUILayout.EnumPopup("Axis", SceneData.currentAxis);
                    EditorGUILayout.MinMaxSlider($"Range [{SceneData.minAngle:N0}-{SceneData.maxAngle:N0}]", ref SceneData.minAngle, ref SceneData.maxAngle, 0f, 360f);
                }
                else
                {
                    SceneData.rotation = EditorGUILayout.Vector3Field("Rotation", SceneData.rotation);
                    SceneData.angleTab = EditorGUILayout.FloatField("Rotation Delta", SceneData.angleTab);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical("Box");
                SceneData.randomizeScale = EditorGUILayout.Toggle("Randomize Scale", SceneData.randomizeScale);
                if (SceneData.randomizeScale)
                {
                    EditorGUILayout.BeginHorizontal();
                    SceneData.minScale = EditorGUILayout.FloatField("Min", SceneData.minScale);
                    SceneData.maxScale = EditorGUILayout.FloatField("Max", SceneData.maxScale);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    SceneData.scale = EditorGUILayout.Vector3Field("Scale", SceneData.scale);
                }
                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void GridGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            if (gridFoldout = EditorGUILayout.Foldout(gridFoldout, "Grid"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Draw XY", GUILayout.Width(60));
                SceneData.drawXY = GUILayout.Toggle(SceneData.drawXY, "", GUILayout.Width(20));
                GUILayout.Space(40);
                GUILayout.Label("Draw XZ", GUILayout.Width(60));
                SceneData.drawXZ = GUILayout.Toggle(SceneData.drawXZ, "", GUILayout.Width(20));
                GUILayout.Space(40);
                GUILayout.Label("Draw YZ", GUILayout.Width(60));
                SceneData.drawYZ = GUILayout.Toggle(SceneData.drawYZ, "", GUILayout.Width(20));
                GUILayout.EndHorizontal();

                SceneData.gridSize = EditorGUILayout.IntField("Size", SceneData.gridSize);
                if (SceneData.gridSize < 0) SceneData.gridSize = 0;
            }
            EditorGUILayout.EndVertical();
        }

        private void SnappingGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            if (snapFoldout = EditorGUILayout.Foldout(snapFoldout, "Snapping"))
            {
                bool oldEnabled = GUI.enabled;

                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal();

                GUILayout.Label("Snap X", GUILayout.Width(45));
                SceneData.snapX = GUILayout.Toggle(SceneData.snapX, "", GUILayout.Width(20));
                if (SceneData.snapX) SceneData.autoSetX = false;
                GUILayout.Space(40);
                GUILayout.Label("Snap Y", GUILayout.Width(45));
                SceneData.snapY = GUILayout.Toggle(SceneData.snapY, "", GUILayout.Width(20));
                if (SceneData.snapY) SceneData.autoSetY = false;
                GUILayout.Space(40);
                GUILayout.Label("Snap Z", GUILayout.Width(45));
                SceneData.snapZ = GUILayout.Toggle(SceneData.snapZ, "", GUILayout.Width(20));
                if (SceneData.snapZ) SceneData.autoSetZ = false;

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Autoset X", GUILayout.MaxWidth(60));
                GUI.enabled = (!SceneData.snapX);
                SceneData.autoSetX = GUILayout.Toggle(SceneData.autoSetX, "", GUILayout.Width(20));
                GUI.enabled = SceneData.autoSetX;
                SceneData.autoXPos = EditorGUILayout.FloatField(SceneData.autoXPos);
                GUILayout.EndHorizontal();

                GUI.enabled = oldEnabled;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Autoset Y", GUILayout.MaxWidth(60));
                GUI.enabled = (!SceneData.snapY);
                SceneData.autoSetY = GUILayout.Toggle(SceneData.autoSetY, "", GUILayout.Width(20));
                GUI.enabled = SceneData.autoSetY;
                SceneData.autoYPos = EditorGUILayout.FloatField(SceneData.autoYPos);
                GUILayout.EndHorizontal();

                GUI.enabled = oldEnabled;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Autoset Z", GUILayout.MaxWidth(60));
                GUI.enabled = (!SceneData.snapZ);
                SceneData.autoSetZ = GUILayout.Toggle(SceneData.autoSetZ, "", GUILayout.Width(20));
                GUI.enabled = SceneData.autoSetZ;
                SceneData.autoZPos = EditorGUILayout.FloatField(SceneData.autoZPos);
                GUILayout.EndHorizontal();

                GUI.enabled = oldEnabled;

                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Placing

        private void PlaceGameObject()
        {
            if (!root) return;

            GameObject original = null;

            if (SceneData.selectedGroup != null && SceneData.selectedGroup.useRandom) original = SceneData.selectedGroup.RandomGameObject();
            else if (SceneData.selected != null) original = SceneData.selected.go;

            if (!original)
            {
                Debug.Log("No gameObject selected!");
                return;
            }

            GameObject instance;
            if (PrefabUtility.GetPrefabAssetType(original) == PrefabAssetType.NotAPrefab) instance = Instantiate(original);
            else instance = PrefabUtility.InstantiatePrefab(original) as GameObject;

            if (SceneData.randomizeScale) instance.transform.localScale = original.transform.localScale * Random.Range(SceneData.minScale, SceneData.maxScale);
            else instance.transform.localScale = new(original.transform.localScale.x * SceneData.scale.x, original.transform.localScale.y * SceneData.scale.y, original.transform.localScale.z * SceneData.scale.z);

            instance.transform.position = hitPoint + SceneData.offset;

            if (SceneData.useNormals)
            {
                instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
                if (SceneData.randomizeRotation && SceneData.currentAxis == Axis.Y)
                {
                    instance.transform.RotateAround(instance.transform.position, hitNormal, Random.Range(SceneData.minAngle, SceneData.maxAngle));
                }
            }
            else
            {
                if (SceneData.randomizeRotation) instance.transform.rotation = GetRandomAngle();
                else instance.transform.rotation = Quaternion.Euler(SceneData.rotation);
            }

            if (SceneData.selectedGroup != null)
            {
                if (SceneData.selectedGroup.parent)
                {
                    instance.transform.SetParent(SceneData.selectedGroup.parent);
                }
                else
                {
                    Transform groupTransform = root.Find(SceneData.selectedGroup.name);
                    if (groupTransform) instance.transform.parent = groupTransform;
                    else
                    {
                        GameObject newParent = new(SceneData.selectedGroup.name);
                        newParent.transform.SetParent(root);
                        newParent.transform.localPosition = Vector3.zero;
                        instance.transform.SetParent(newParent.transform);
                        Undo.RegisterCreatedObjectUndo(newParent, "Instantiated object parent");
                    }
                }
            }
            Undo.RegisterCreatedObjectUndo(instance, "Instantiated object");
        }

        private void SetTargetPoint(Event e)
        {
            root = null;
            Camera cam = Camera.current;
            if (cam != null)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(new Vector2(e.mousePosition.x, e.mousePosition.y));

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, SceneData.RayMask))
                {
                    root = hit.transform.root;
                    hitPoint = SnapToCustomGrid(hit.point);
                    hitNormal = hit.normal;
                }
                else
                {
                    hitPoint = SnapToCustomGrid(cam.ScreenToWorldPoint(new Vector3(e.mousePosition.x, cam.pixelHeight - e.mousePosition.y, 20)));
                    hitNormal = Vector3.up;
                }
            }
        }

        private void SetTargetVisualization()
        {
            if (!root) return;

            Color oldColor = Handles.color;
            Handles.color = new Color(1, 0, 0, .5f);
            Handles.DrawSolidDisc(hitPoint, hitNormal, .25f);
            Handles.color = oldColor;
        }

        private void AddRotation()
        {
            SceneData.startingRot = SceneData.angleTab + SceneData.rotation.y;
            if (SceneData.startingRot >= 360f) SceneData.startingRot -= 360f;
            SceneData.rotation = new Vector3(0f, SceneData.startingRot, 0);
        }

        private Quaternion GetRandomAngle()
        {
            float random = Random.Range(SceneData.minAngle, SceneData.maxAngle);

            return SceneData.currentAxis switch
            {
                Axis.X => Quaternion.Euler(random, 0, 0),
                Axis.Y => Quaternion.Euler(0, random, 0),
                Axis.Z => Quaternion.Euler(0, 0, random),
                _ => Quaternion.identity,
            };
        }

        #endregion

        #region Snapping

        private float SnapNumber(float unitSize, float numToSnap)
        {
            return Mathf.Round(numToSnap / unitSize) * unitSize;
        }
        private float SnapToHalfUnit(float unitSize, float numToSnap)
        {
            return Mathf.Floor(numToSnap / unitSize) + .5f * unitSize;
        }
        private Vector3 SnapToCustomGrid(Vector3 positionToSnap)
        {
            //		if(gridTransform.objectReferenceValue == null)
            //			return Vector3.zero;

            float x = positionToSnap.x;
            float y = positionToSnap.y;
            float z = positionToSnap.z;

            bool resetXinWorldSpace = false;
            bool resetYinWorldSpace = false;
            bool resetZinWorldSpace = false;

            if (SceneData.snapX) x = SnapNumber(1, x);
            if (SceneData.snapY) y = SnapNumber(1, y);
            if (SceneData.snapZ) z = SnapNumber(1, z);

            if (SceneData.autoSetX)
            {
                resetXinWorldSpace = true;
                x = SceneData.autoXPos * 1;
            }
            if (SceneData.autoSetY)
            {
                resetYinWorldSpace = true;
                y = SceneData.autoYPos * 1;
            }
            if (SceneData.autoSetZ)
            {
                resetZinWorldSpace = true;
                z = SceneData.autoZPos * 1;
            }

            Vector3 snappedPoint = new(x, y, z);

            if (!(resetXinWorldSpace || resetYinWorldSpace || resetZinWorldSpace)) return snappedPoint;

            if (SceneData.autoSetX)
            {
                snappedPoint = new(SceneData.autoXPos, snappedPoint.y, snappedPoint.z);
            }
            if (SceneData.autoSetY)
            {
                snappedPoint = new(snappedPoint.x, SceneData.autoYPos, snappedPoint.z);
            }
            if (SceneData.autoSetZ)
            {
                snappedPoint = new(snappedPoint.x, snappedPoint.y, SceneData.autoZPos);
            }

            return snappedPoint;
        }

        #endregion
    }
}
#endif
