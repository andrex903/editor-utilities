#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class AnimationsUtilities
    {
        [MenuItem("Assets/Redeev/Create Triggers", validate = true)]
        private static bool CreateTriggersValidation()
        {
            if (Selection.objects.Length == 1)
            {
                return Selection.objects[0].GetType() == typeof(AnimatorController);
            }
            return false;
        }

        [MenuItem("Assets/Redeev/Create Triggers")]
        private static void CreateTriggers()
        {
            if (!EditorUtility.DisplayDialog("Warning", "This will delete all the current parameters and transitions", "Continue", "Cancel")) return;

            var controller = Selection.objects[0] as AnimatorController;

            controller.parameters = new AnimatorControllerParameter[0];
            controller.layers[0].stateMachine.anyStateTransitions = new AnimatorStateTransition[0];

            var childStates = controller.layers[0].stateMachine.states;
            controller.layers[0].stateMachine.entryPosition = new Vector3(400f, -(childStates.Length / 2 + 1) * 50f, 0f);
            for (int i = 0; i < childStates.Length; i++)
            {
                var state = childStates[i];
                state.position = new(400f, 50f * (i - childStates.Length / 2), 0f);
                childStates[i] = state;
            }
            controller.layers[0].stateMachine.states = childStates;

            foreach (var state in controller.layers[0].stateMachine.states)
            {
                controller.AddParameter(state.state.name, AnimatorControllerParameterType.Trigger);
                var transition = controller.layers[0].stateMachine.AddAnyStateTransition(state.state);
                transition.AddCondition(AnimatorConditionMode.If, 0, state.state.name);
                transition.canTransitionToSelf = false;
                transition.duration = 0;
            }
        }

        [MenuItem("Assets/Redeev/Setup Animation", validate = true)]
        private static bool SetupAnimationValidation()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if (Path.GetExtension(path) != ".fbx") return false;
            }
            return Selection.objects.Length > 0;
        }

        [MenuItem("Assets/Redeev/Setup Animation")]
        private static void SetupAnimation()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if (Path.GetExtension(path) == ".fbx")
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);

                    SetupModelImporter(importer, fileName);
                }
            }
        }

        private static void SetupModelImporter(ModelImporter modelImporter, string name)
        {
            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;

            for (int i = 0; i < clipAnimations.Length; i++)
            {                
                clipAnimations[i].name = name;
                if (name.Contains("Walk") || name.Contains("Idle") || name.Contains("Run")) clipAnimations[i].loopTime = true;
                //if (name.Contains("Attack")) clipAnimations[i].events = new AnimationEvent[1] { new AnimationEvent() { functionName = "InstanceEvent", time = 0.5f } };

                clipAnimations[i].keepOriginalPositionY = true;
                clipAnimations[i].keepOriginalPositionXZ = true;
                clipAnimations[i].keepOriginalOrientation = true;
                clipAnimations[i].lockRootHeightY = true;
                clipAnimations[i].lockRootPositionXZ = true;
                clipAnimations[i].lockRootRotation = true;
            }

            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }
    }
}
#endif