#if UNITY_EDITOR
using UnityEditor;

namespace RedeevEditor.Utilities
{
    public static class EditorExtensions
    {
        [MenuItem("GameObject/Set Parent/Root", true)]
        public static bool MoveToRootParentValidation()
        {
            var selections = Selection.gameObjects;
            foreach (var selection in selections)
            {
                if (!selection.scene.IsValid())
                {
                    return false;
                }
            }
            return true;
        }

        [MenuItem("GameObject/Set Parent/Root")]
        public static void MoveToRootParent()
        {
            var selections = Selection.gameObjects;

            Undo.SetCurrentGroupName("Move gameObjects to root parent");
            int group = Undo.GetCurrentGroup();
            foreach (var selection in selections)
            {
                Undo.SetTransformParent(selection.transform, selection.transform.root, "Move to root parent");
            }
            Undo.CollapseUndoOperations(group);
        }

        [MenuItem("GameObject/Set Parent/Previous", true)]
        public static bool MoveToParentUpValidation()
        {
            return Selection.gameObjects.Length == 1;
        }

        [MenuItem("GameObject/Set Parent/Previous")]
        public static void MoveToParentUp()
        {
            var selections = Selection.gameObjects;

            Undo.SetCurrentGroupName("Move gameObjects up to parent");
            int group = Undo.GetCurrentGroup();
            foreach (var selection in selections)
            {
                Undo.SetTransformParent(selection.transform, selection.transform.parent.parent, "Move up to parent");
            }
            Undo.CollapseUndoOperations(group);
        }
    }
}
#endif