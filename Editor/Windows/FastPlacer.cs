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
        private Vector3 lastPoint;
        private Vector3 hitNormal;
        private Vector3 lastNormal;
        private Transform root;
        private Transform lastRoot;

        private int controlID;
        private bool isActive = false;

        private bool offsetFoldout = false;
        private bool snapFoldout = false;
        private bool gridFoldout = false;
        private bool placingOptionsFoldout = false;

        private Vector2 scrollPos = Vector2.zero;

        private GameObject lastPlaced = null;

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
                        sceneData = new GameObject($"({nameof(FastPlacerSceneData)})").AddComponent<FastPlacerSceneData>();
                        sceneData.gameObject.hideFlags = HideFlags.HideInInspector;
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
            if (state == PlayModeStateChange.EnteredPlayMode) isActive = false;
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

            if (!isActive || evt.alt) return;

            SetTargetPoint(evt);
            SetTargetVisualization();

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
                    AddRotation();
                    Repaint();
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    if (!SceneData.chooseRandom) ChangeSelection(-1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    if (!SceneData.chooseRandom) ChangeSelection(1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.LeftArrow)
                {
                    ChangeLastPlaced(-1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.RightArrow)
                {
                    ChangeLastPlaced(1);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                lastPoint = hitPoint;
                lastNormal = hitNormal;
                lastRoot = root;
                PlaceGameObject();
                evt.Use();
            }
            else if (evt.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
        }

        private void ChangeSelection(int amount)
        {
            if (SceneData.selected != null && SceneData.selectedGroup != null)
            {
                int index = SceneData.selectedGroup.elements.IndexOf(SceneData.selected) + amount;
                if (index >= 0 && index < SceneData.selectedGroup.elements.Count)
                {
                    SceneData.Select(SceneData.selectedGroup.elements[index]);
                    Repaint();
                }
            }
        }

        private void ChangeLastPlaced(int amount)
        {
            if (SceneData.selected == null) return;

            Undo.DestroyObjectImmediate(lastPlaced);
            ChangeSelection(amount);
            PlaceGameObject(SceneData.selected.go);
        }

        private void SetActive(bool value)
        {
            isActive= value;
            lastPlaced = null;
            if (isActive) Selection.objects = new Object[0];
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

            if (GUILayout.Button(isActive ? "Stop" : "Start", GUILayout.Height(30)))
            {
               SetActive(!isActive);
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
            EditorGUILayout.EndScrollView();
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
            SceneData.chooseRandom = EditorGUILayout.Toggle("Randomize Selection", SceneData.chooseRandom);

            for (int i = 0; i < SceneData.groups.Count; i++)
            {
                EditorGUILayout.BeginVertical("Box");
                if (SceneData.groups[i].name.Equals(string.Empty)) SceneData.groups[i].name = "Group" + i;
                string title = (SceneData.selectedGroup == SceneData.groups[i]) ? SceneData.groups[i].name + " (Active)" : SceneData.groups[i].name;

                if (SceneData.groups[i].isOpen = EditorGUILayout.Foldout(SceneData.groups[i].isOpen, title, EditorStyles.foldout))
                {
                    GUILayout.BeginVertical();
                    if (SceneData.selectedGroup != SceneData.groups[i] && GUILayout.Button("Activate")) SceneData.selectedGroup = SceneData.groups[i];
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
            Rect rect = EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.LabelField("GameObjects:", EditorStyles.boldLabel);

            EditorGUILayout.Space(1);

            foreach (var element in group.elements)
            {
                if (SceneData.selected == element && !element.go)
                {
                    SceneData.Deselect();
                }
                EditorGUILayout.BeginHorizontal();

                bool oldEnabled = GUI.enabled;
                GUI.enabled = element.go && !SceneData.chooseRandom;
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

            EditorUtilityGUI.DropAreaGUI(rect, obj =>
            {
                group.elements.Add(new Element() { go = obj as GameObject });
            });
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
                SceneData.space = (Space)EditorGUILayout.EnumPopup("Space", SceneData.space);
                SceneData.currentAxis = (Axis)EditorGUILayout.EnumPopup("Axis", SceneData.currentAxis);
                SceneData.angleTab = EditorGUILayout.FloatField("Rotation Delta", SceneData.angleTab);
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
            if (!lastRoot) return;

            GameObject original = null;

            if (SceneData.selectedGroup != null && SceneData.chooseRandom)
            {
                SceneData.Select(SceneData.selectedGroup.elements[Random.Range(0, SceneData.selectedGroup.elements.Count)]);
                Repaint();
            }

            if (SceneData.selected != null) original = SceneData.selected.go;

            PlaceGameObject(original);
        }

        private void PlaceGameObject(GameObject original)
        {
            if (!original)
            {
                Debug.Log("No gameObject selected!");
                return;
            }

            Selection.objects = new Object[0];

            GameObject instance;
            if (PrefabUtility.GetPrefabAssetType(original) == PrefabAssetType.NotAPrefab) instance = Instantiate(original);
            else instance = PrefabUtility.InstantiatePrefab(original) as GameObject;


            if (SceneData.randomizeScale) instance.transform.localScale = original.transform.localScale * Random.Range(SceneData.minScale, SceneData.maxScale);
            else instance.transform.localScale = new(original.transform.localScale.x * SceneData.scale.x, original.transform.localScale.y * SceneData.scale.y, original.transform.localScale.z * SceneData.scale.z);

            instance.transform.position = lastPoint + SceneData.offset;

            if (SceneData.useNormals)
            {
                instance.transform.rotation = Quaternion.FromToRotation(GetAxisVector(1f), lastNormal);
                if (SceneData.randomizeRotation)
                {
                    instance.transform.RotateAround(instance.transform.position, lastNormal, Random.Range(SceneData.minAngle, SceneData.maxAngle));
                }
            }
            else
            {
                if (SceneData.randomizeRotation) instance.transform.Rotate(GetRandomAngle(), SceneData.space);
                else instance.transform.Rotate(SceneData.rotation, SceneData.space);
            }

            if (SceneData.selectedGroup != null)
            {
                if (SceneData.selectedGroup.parent)
                {
                    instance.transform.SetParent(SceneData.selectedGroup.parent);
                }
                else
                {
                    Transform groupTransform = lastRoot.Find(SceneData.selectedGroup.name);
                    if (groupTransform) instance.transform.parent = groupTransform;
                    else
                    {
                        GameObject newParent = new(SceneData.selectedGroup.name);
                        newParent.transform.SetParent(lastRoot);
                        newParent.transform.localPosition = Vector3.zero;
                        instance.transform.SetParent(newParent.transform);
                        Undo.RegisterCreatedObjectUndo(newParent, "Instantiated object parent");
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(instance, "Instantiated object");
            lastPlaced = instance;
        }

        private void SetTargetPoint(Event evt)
        {
            root = null;
            Camera cam = Camera.current;
            if (cam != null)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(new(evt.mousePosition.x, evt.mousePosition.y));

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, SceneData.RayMask))
                {
                    root = hit.transform.root;
                    hitPoint = SnapToCustomGrid(hit.point);
                    hitNormal = hit.normal;
                }
                else
                {
                    hitPoint = SnapToCustomGrid(cam.ScreenToWorldPoint(new(evt.mousePosition.x, cam.pixelHeight - evt.mousePosition.y, 20)));
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
            Vector3 delta = GetAxisVector(sceneData.angleTab);
            SceneData.rotation += delta;
            if (SceneData.rotation.x > 360f) SceneData.rotation.x -= 360f;
            if (SceneData.rotation.y > 360f) SceneData.rotation.y -= 360f;
            if (SceneData.rotation.z > 360f) SceneData.rotation.z -= 360f;

            if (lastPlaced != null)
            {
                lastPlaced.transform.Rotate(delta, SceneData.space);
            }
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
