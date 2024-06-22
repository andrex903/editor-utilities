#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedeevEditor.Utilities
{
    [InitializeOnLoad]
    public static class ToolbarExtension
    {
        public static readonly List<Action> LeftToolbarGUI = new();
        public static readonly List<Action> RightToolbarGUI = new();

        static ToolbarExtension()
        {
            ToolbarCallback.OnToolbarGUILeft = OnToolbarGUILeft;
            ToolbarCallback.OnToolbarGUIRight = OnToolbarGUIRight;
        }

        public static void OnToolbarGUILeft()
        {
            OnToolbarGUI(LeftToolbarGUI);
        }

        public static void OnToolbarGUIRight()
        {
            OnToolbarGUI(RightToolbarGUI);
        }

        private static void OnToolbarGUI(List<Action> handlers)
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in handlers)
            {
                handler?.Invoke();
            }
            GUILayout.EndHorizontal();
        }
    }

    public static class ToolbarCallback
    {
        private readonly static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject currentToolbar;

        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            // Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
            if (currentToolbar != null) return; 

            var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);

            currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (currentToolbar != null)
            {
                var root = currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                var rawRoot = root.GetValue(currentToolbar);
                var mRoot = rawRoot as VisualElement;
                RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
                RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

                void RegisterCallback(string root, Action action)
                {
                    var toolbarZone = mRoot.Q(root);

                    var parent = new VisualElement()
                    {
                        style = { flexGrow = 1, flexDirection = FlexDirection.Row, }
                    };
                    var container = new IMGUIContainer();
                    container.style.flexGrow = 1;
                    container.onGUIHandler += action;
                    parent.Add(container);
                    toolbarZone.Add(parent);
                }
            }
        }
    }
}
#endif