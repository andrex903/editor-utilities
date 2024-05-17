#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static RedeevEditor.Utilities.FastPlacerSceneData;

namespace RedeevEditor.Utilities
{
    public class FastPlacer : EditorWindow
    {
        private class HitInfo
        {
            public Transform root;
            public Vector3 point;
            public Vector3 normal;

            public HitInfo(Transform root, Vector3 point, Vector3 normal)
            {
                this.root = root;
                this.point = point;
                this.normal = normal;
            }
        }

        private HitInfo currentHit;
        private HitInfo clickHit;

        private int controlID;
        private bool isActive = false;

        private bool offsetFoldout = false;
        private bool previewFoldout = false;
        private bool snapFoldout = false;
        private bool gridFoldout = false;
        private bool placingOptionsFoldout = false;
        private bool groupsFoldout = false;

        private Vector2 scrollPos = Vector2.zero;

        private GameObject preview = null;
        private GameObject selectedPrefab = null;

        private Vector3 lastRotation;

        private KeyValuePair<GameObject, Texture2D> currentPreview;

        private FastPlacerSceneData sceneData = null;
        private readonly Collider[] colliders = new Collider[1];

        private FastPlacerSceneData SceneData
        {
            get
            {
                if (sceneData == null)
                {
                    sceneData = FindObjectOfType<FastPlacerSceneData>();
                    if (sceneData == null)
                    {
                        sceneData = new GameObject($"({nameof(FastPlacerSceneData)})").AddComponent<FastPlacerSceneData>();
                        sceneData.gameObject.hideFlags = HideFlags.HideInInspector;
                    }
                }
                return sceneData;
            }
        }

        private readonly int HASH = "FastPlacer".GetHashCode();
        private const string CREATE_ICON = "d_Toolbar Plus";
        private const string CREATE_GROUP_ICON = "Add-Available";
        private const string GROUP_ICON = "d_FolderEmpty Icon";
        private const string ACTIVE_GROUP_ICON = "d_Folder Icon";
        private const string DELETE_ICON = "TreeEditor.Trash";

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

        private void OnDisable()
        {
            SetActive(false);
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) isActive = false;
        }

        private void SetActive(bool value)
        {
            isActive = value;

            Selection.objects = new Object[0];
            Tools.hidden = value;

            if (value)
            {
                SelectPrefab();
                CreatePreview(new(null, Vector3.zero, Vector3.up));
            }
            else
            {
                DestroyPreview();
            }
        }

        #region Generic

        private HitInfo GetHitInformations(Event evt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(new(evt.mousePosition.x, evt.mousePosition.y));
            return GetHitInformations(ray);
        }

        private HitInfo GetHitInformations(Vector3 point)
        {
            Ray ray = new(point + Vector3.up * 10f, -Vector3.up);
            return GetHitInformations(ray);
        }

        private HitInfo GetHitInformations(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, SceneData.RayMask))
            {
                return new(hit.transform.root, SnapToCustomGrid(hit.point), hit.normal);
            }

            return null;
        }

        private void DrawTarget()
        {
            if (currentHit == null) return;

            Color oldColor = Handles.color;
            Handles.color = new Color(1, 0, 0, .5f);
            Handles.DrawSolidDisc(currentHit.point, currentHit.normal, SceneData.BrushSize);
            Handles.color = oldColor;
        }

        private Vector3 GetRandomAngle()
        {
            return GetAxisVector(Random.Range(SceneData.minAngle, SceneData.maxAngle));
        }

        private Vector3 GetAxisVector(float value)
        {
            return SceneData.currentAxis switch
            {
                Axis.X => new Vector3(value, 0, 0),
                Axis.Y => new Vector3(0, value, 0),
                Axis.Z => new Vector3(0, 0, value),
                _ => Vector3.zero,
            };
        }

        private void Align(Transform transform, Vector3 target)
        {
            switch (SceneData.currentAxis)
            {
                case Axis.X:
                    transform.right = target;
                    break;
                case Axis.Y:
                    transform.up = target;
                    break;
                case Axis.Z:
                    transform.forward = target;
                    break;
                default:
                    break;
            }
        }

        private Space GetSpace()
        {
            return SceneData.alignWithNormals ? Space.Self : SceneData.space;
        }

        #endregion

        #region GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            Event evt = Event.current;
            controlID = GUIUtility.GetControlID(HASH, FocusType.Passive);

            if (!isActive || evt.alt) return;

            currentHit = GetHitInformations(evt);
            if (SceneData.showPreview && SceneData.paintMode == PaintMode.Single)
            {
                DrawPreview();
            }
            else DrawTarget();

            if (evt.GetTypeForControl(controlID) == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    SetActive(false);
                    Repaint();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Space)
                {
                    ChangeRotation();
                    Repaint();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    if (!SceneData.randomizeSelection) ChangeSelection(-1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    if (!SceneData.randomizeSelection) ChangeSelection(1);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                clickHit = currentHit;
                if (clickHit != null)
                {
                    if (SceneData.paintMode == PaintMode.Single) PlaceGameObject();
                    else PlaceGameObjects();
                }

                if (SceneData.randomizeSelection) SelectRandomPrefab();
                CreatePreview(clickHit);

                evt.Use();
            }
            else if (evt.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Not available in playMode");
                return;
            }

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = isActive ? Color.red : Color.green;

            if (GUILayout.Button(new GUIContent(isActive ? " Stop" : " Paint", EditorGUIUtility.IconContent("d_Grid.PaintTool").image), GUILayout.Height(25f)))
            {
                SetActive(!isActive);
                ActivateCollider(isActive);
            }

            GUI.backgroundColor = oldColor;

            MeshPreviewGUI();

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
            EditorGUILayout.EndScrollView();
        }

        private void PlacingOptionsGUI()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            if (placingOptionsFoldout = EditorGUILayout.Foldout(placingOptionsFoldout, "Placing Options"))
            {
                EditorGUI.indentLevel++;
                SceneData.randomizeSelection = EditorGUILayout.Toggle("Randomize Selection", SceneData.randomizeSelection);
                LayerMask tempMask = EditorGUILayout.MaskField("Raycast Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(SceneData.RayMask), InternalEditorUtility.layers);
                SceneData.RayMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                SceneData.alignWithNormals = EditorGUILayout.Toggle("Align with Normals", SceneData.alignWithNormals);
                if (SceneData.paintMode == PaintMode.Multi) GUI.enabled = false;
                SceneData.showPreview = EditorGUILayout.Toggle("Show Preview", SceneData.showPreview);
                if (sceneData.showPreview)
                {
                    sceneData.showPreviewGizmos = EditorGUILayout.Toggle("Show Preview Gizmos", SceneData.showPreviewGizmos);
                }
                GUI.enabled = true;
                if (!SceneData.showPreview) DestroyPreview();
                SceneData.paintMode = (PaintMode)EditorGUILayout.EnumPopup("Paint Mode", SceneData.paintMode);
                if (SceneData.paintMode == PaintMode.Multi)
                {
                    EditorGUILayout.BeginVertical("Box");
                    SceneData.brushSize = EditorGUILayout.Slider("Brush Size", SceneData.brushSize, 0.25f, 5f);
                    SceneData.density = EditorGUILayout.FloatField("Density", SceneData.density);
                    SceneData.minDistance = EditorGUILayout.FloatField("Min Distance", SceneData.minDistance);
                    SceneData.useColliders = EditorGUILayout.Toggle("Use Colliders", SceneData.useColliders);
                    LayerMask tempMask2 = EditorGUILayout.MaskField("Collider Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(SceneData.collidersMask), InternalEditorUtility.layers);
                    SceneData.collidersMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void GroupsGUI()
        {
            EditorGUILayout.BeginVertical("HelpBox");

            if (groupsFoldout = EditorGUILayout.Foldout(groupsFoldout, "Groups"))
            {
                for (int i = 0; i < SceneData.groups.Count; i++)
                {
                    EditorGUILayout.BeginVertical("Box");
                    if (SceneData.groups[i].name.Equals(string.Empty)) SceneData.groups[i].name = "Group" + i;
                    string title = SceneData.groups[i].name;
                    string groupIcon = (SceneData.selectedGroup == SceneData.groups[i]) ? ACTIVE_GROUP_ICON : GROUP_ICON;
                    if (SceneData.groups[i].isOpen = EditorGUILayout.Foldout(SceneData.groups[i].isOpen, new GUIContent(title, EditorGUIUtility.IconContent(groupIcon).image), EditorStyles.foldout))
                    {
                        GUILayout.BeginVertical();

                        if (SceneData.selectedGroup != SceneData.groups[i])
                        {
                            if (GUILayout.Button("Activate"))
                            {
                                SceneData.Deselect();
                                selectedPrefab = null;
                                SceneData.selectedGroup = SceneData.groups[i];
                            }
                        }
                        else
                        {
                            Color oldColor = GUI.backgroundColor;
                            GUI.backgroundColor = Color.cyan;
                            GUILayout.Button("Active");
                            GUI.backgroundColor = oldColor;
                        }

                        GUILayout.BeginHorizontal();
                        SceneData.groups[i].name = EditorGUILayout.TextField("Name", SceneData.groups[i].name);
                        if (SceneData.groups[i].name.Equals(string.Empty)) SceneData.groups[i].name = "Group " + i;

                        if (EditorUtilityGUI.IconButton(DELETE_ICON, 30f))
                        {
                            if (EditorUtility.DisplayDialog("Group Deletion", "Are you sure to delete this group?", "Confirm", "Cancel"))
                            {
                                if (SceneData.groups[i] == SceneData.selectedGroup) SceneData.Deselect();
                                SceneData.groups.RemoveAt(i);
                                GUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.EndVertical();
                                break;
                            }
                        }
                        GUILayout.EndHorizontal();

                        SceneData.groups[i].parent = EditorGUILayout.ObjectField("Container", SceneData.groups[i].parent, typeof(Transform), true) as Transform;

                        GroupElementsGUI(SceneData.groups[i]);

                        GUILayout.EndVertical();
                    }
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent(CREATE_GROUP_ICON))) SceneData.groups.Add(new Group());
            }
            EditorGUILayout.EndVertical();
        }

        private void GroupElementsGUI(Group group)
        {
            Rect rect = EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.Space(1f);
            if (group.elements.Count == 0)
            {
                EditorGUILayout.LabelField("Add or drag an object here", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(20f));
            }
            foreach (var element in group.elements)
            {
                if (SceneData.selected == element && !element.go)
                {
                    SceneData.Deselect();
                }
                EditorGUILayout.BeginHorizontal();

                bool oldEnabled = GUI.enabled;
                GUI.enabled = element.go && !SceneData.randomizeSelection;
                element.isSelected = GUILayout.Toggle(element.isSelected, "", GUILayout.Width(20));
                GUI.enabled = oldEnabled;
                if (element.isSelected)
                {
                    SceneData.Select(element);
                    SelectPrefab();
                }
                else if (sceneData.selected == element) SceneData.Deselect();

                element.go = EditorGUILayout.ObjectField(element.go, typeof(GameObject), true) as GameObject;

                if (EditorUtilityGUI.IconButton(DELETE_ICON, 20f))
                {
                    group.elements.Remove(element);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(1f);
            if (GUILayout.Button(EditorGUIUtility.IconContent(CREATE_ICON)))
            {
                group.elements.Add(new Element());
            }
            EditorGUILayout.EndVertical();

            EditorUtilityGUI.DropAreaGUI(rect, obj =>
            {
                group.elements.Add(new Element() { go = obj as GameObject });
            });
        }

        private void OffsetRoationAndScaleGUI()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            if (offsetFoldout = EditorGUILayout.Foldout(offsetFoldout, "Transform"))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("Box");
                SceneData.positionOffset = EditorGUILayout.Vector3Field("Position Offset", SceneData.positionOffset);
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical("Box");
                if (!SceneData.alignWithNormals) SceneData.space = (Space)EditorGUILayout.EnumPopup("Space", SceneData.space);
                SceneData.currentAxis = (Axis)EditorGUILayout.EnumPopup("Axis", SceneData.currentAxis);
                SceneData.rotationDelta = EditorGUILayout.FloatField("Rotation Delta", SceneData.rotationDelta);
                SceneData.randomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", SceneData.randomizeRotation);
                if (SceneData.randomizeRotation)
                {
                    EditorGUILayout.MinMaxSlider($"Range [{SceneData.minAngle:N0}-{SceneData.maxAngle:N0}]", ref SceneData.minAngle, ref SceneData.maxAngle, 0f, 360f);
                }
                else
                {
                    SceneData.rotation = EditorGUILayout.Vector3Field("Rotation", SceneData.rotation);
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
            EditorGUILayout.BeginVertical("HelpBox");
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
            EditorGUILayout.BeginVertical("HelpBox");
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

        private void MeshPreviewGUI()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            if (previewFoldout = EditorGUILayout.Foldout(previewFoldout, $"Preview"))
            {
                Rect rect = GUILayoutUtility.GetRect(position.width * 0.3f, position.width * 0.3f);

                if (SceneData.selected != null && sceneData.selected.go != null)
                {
                    Texture2D preview;
                    if (currentPreview.Key == sceneData.selected.go && currentPreview.Value != null)
                    {
                        preview = currentPreview.Value;
                    }
                    else
                    {
                        preview = AssetPreview.GetAssetPreview(sceneData.selected.go);
                        currentPreview = new(sceneData.selected.go, preview);
                    }

                    if (preview != null)
                    {
                        EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit, 0f);
                    }
                }

            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Editing 

        private void ChangeSelection(int amount)
        {
            if (SceneData.selected != null && SceneData.selectedGroup != null)
            {
                int index = SceneData.selectedGroup.elements.IndexOf(SceneData.selected) + amount;
                if (index >= 0 && index < SceneData.selectedGroup.elements.Count)
                {
                    SceneData.Select(SceneData.selectedGroup.elements[index]);
                    Repaint();
                    SelectPrefab();
                    CreatePreview(currentHit);
                }
            }
        }

        private void ChangeRotation()
        {
            Vector3 delta = GetAxisVector(sceneData.rotationDelta);
            SceneData.rotation += delta;
            if (SceneData.rotation.x > 360f) SceneData.rotation.x -= 360f;
            if (SceneData.rotation.y > 360f) SceneData.rotation.y -= 360f;
            if (SceneData.rotation.z > 360f) SceneData.rotation.z -= 360f;

            CreatePreview(currentHit);
        }

        #endregion

        #region Preview

        private void CreatePreview(HitInfo hit)
        {
            DestroyPreview();

            if (SceneData.showPreview && selectedPrefab)
            {
                preview = InstantiateObject(selectedPrefab, hit);
                if (sceneData.showPreviewGizmos)
                {
                    preview.name = "(_Preview)";
                    preview.hideFlags = HideFlags.DontSave;
                }
                else preview.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void DestroyPreview()
        {
            if (preview) DestroyImmediate(preview);
        }

        private void DrawPreview()
        {
            if (!preview) return;

            if (currentHit != null)
            {
                preview.SetActive(true);
                SetPosition(preview, currentHit);

                if (SceneData.alignWithNormals)
                {
                    SetNormal(preview, currentHit);
                    preview.transform.Rotate(lastRotation, GetSpace());
                }
            }
            else
            {
                preview.SetActive(false);
            }
        }

        #endregion

        #region Selection

        private void SelectRandomPrefab()
        {
            if (SceneData.selectedGroup != null && SceneData.randomizeSelection)
            {
                SceneData.Select(SceneData.selectedGroup.elements[Random.Range(0, SceneData.selectedGroup.elements.Count)]);
                Repaint();
            }
            if (SceneData.selected != null) SelectPrefab();
        }

        private void SelectPrefab()
        {
            if (SceneData.selected != null) selectedPrefab = SceneData.selected.go;
        }

        #endregion

        #region Placing

        private void PlaceGameObject()
        {
            if (!selectedPrefab || SceneData.selectedGroup == null) return;

            GameObject instance = InstantiateObject(selectedPrefab);
            if (!instance) return;

            if (preview)
            {
                instance.transform.SetPositionAndRotation(preview.transform.position, preview.transform.rotation);
                instance.transform.localScale = preview.transform.localScale;
            }
            else
            {
                SetScale(instance, selectedPrefab.transform.localScale);
                SetPosition(instance, clickHit);
                SetRotation(instance, clickHit);
            }

            SetParent(instance, clickHit);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiated object");

            Selection.activeGameObject = instance;
        }

        private void PlaceGameObjects()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Instantiated objects");
            var undoGroupIndex = Undo.GetCurrentGroup();

            List<Vector3> points = new();
            int count = CalculateNumber();
            for (int i = 0; i < count; i++)
            {
                if (TryGetGoodPosition(out Vector3 goodPosition))
                {
                    HitInfo hit = GetHitInformations(goodPosition);

                    if (SceneData.useColliders)
                    {
                        if (Physics.OverlapSphereNonAlloc(hit.point, SceneData.minDistance, colliders, SceneData.collidersMask) > 0) continue;
                    }

                    GameObject instance = InstantiateObject(selectedPrefab, hit);
                    if (instance)
                    {
                        if (SceneData.useColliders)
                        {
                            FastPlacerCollider placeComponent = instance.AddComponent<FastPlacerCollider>();
                            placeComponent.Activate(true, SceneData.minDistance, SceneData.collidersMask);
                        }
                        SetParent(instance, clickHit);
                        Undo.RegisterCreatedObjectUndo(instance, string.Empty);
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);

            bool TryGetGoodPosition(out Vector3 point)
            {
                Vector2 pos = Random.insideUnitCircle * SceneData.brushSize;
                point = clickHit.point + new Vector3(pos.x, 0f, pos.y);

                if (!SceneData.useColliders)
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (Vector3.Distance(points[i], point) < SceneData.minDistance) return false;
                    }
                    points.Add(point);
                }
              
                return true;
            }

            int CalculateNumber()
            {
                return Mathf.RoundToInt(SceneData.brushSize * SceneData.brushSize * SceneData.density * 2 * Mathf.PI);
            }
        }

        private void SetParent(GameObject instance, HitInfo hit)
        {
            if (!instance) return;

            if (SceneData.selectedGroup != null && hit != null)
            {
                if (SceneData.selectedGroup.parent)
                {
                    instance.transform.SetParent(SceneData.selectedGroup.parent);
                }
                else if (hit.root)
                {
                    Transform groupTransform = hit.root.Find(SceneData.selectedGroup.name);
                    if (groupTransform) instance.transform.parent = groupTransform;
                    else
                    {
                        GameObject newParent = new(SceneData.selectedGroup.name);
                        newParent.transform.SetParent(hit.root);
                        newParent.transform.localPosition = Vector3.zero;
                        instance.transform.SetParent(newParent.transform);
                        Undo.RegisterCreatedObjectUndo(newParent, "Instantiated group object parent");
                    }
                }
            }
        }

        private GameObject InstantiateObject(GameObject original)
        {
            if (!original) return null;

            GameObject instance;
            if (PrefabUtility.GetPrefabAssetType(original) == PrefabAssetType.NotAPrefab) instance = Instantiate(original);
            else instance = PrefabUtility.InstantiatePrefab(original) as GameObject;

            return instance;
        }

        private GameObject InstantiateObject(GameObject original, HitInfo hit)
        {
            if (hit == null) return null;

            GameObject instance = InstantiateObject(original);
            if (instance != null)
            {
                SetScale(instance, original.transform.localScale);
                SetPosition(instance, hit);
                SetRotation(instance, hit);
            }

            return instance;
        }

        private void SetPosition(GameObject instance, HitInfo hit)
        {
            instance.transform.position = hit.point + SceneData.positionOffset;
        }

        private void SetRotation(GameObject instance, HitInfo hit)
        {
            SetNormal(instance, hit);

            if (SceneData.randomizeRotation)
            {
                lastRotation = GetRandomAngle();
                instance.transform.Rotate(lastRotation, GetSpace());
            }
            else
            {
                lastRotation = sceneData.rotation;
                instance.transform.Rotate(SceneData.rotation, GetSpace());
            }
        }

        private void SetNormal(GameObject instance, HitInfo hit)
        {
            if (SceneData.alignWithNormals)
            {
                Align(instance.transform, hit.normal);
            }
        }

        private void SetScale(GameObject instance, Vector3 originalScale)
        {
            if (SceneData.randomizeScale) instance.transform.localScale = originalScale * Random.Range(SceneData.minScale, SceneData.maxScale);
            else instance.transform.localScale = new(originalScale.x * SceneData.scale.x, originalScale.y * SceneData.scale.y, originalScale.z * SceneData.scale.z);
        }

        #endregion

        #region Snapping

        private float SnapNumber(float unitSize, float numToSnap)
        {
            return Mathf.Round(numToSnap / unitSize) * unitSize;
        }

        private Vector3 SnapToCustomGrid(Vector3 positionToSnap)
        {
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

        #region Grass

        private void ActivateCollider(bool value)
        {
            foreach (var item in FindObjectsOfType<FastPlacerCollider>())
            {
                item.Activate(value, SceneData.minDistance, SceneData.collidersMask);
            }
        }

        #endregion
    }
}
#endif