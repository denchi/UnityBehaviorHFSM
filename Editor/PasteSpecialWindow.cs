using Behaviours.HFSM.Runtime;
using UnityEngine;
using UnityEditor;

namespace Behaviours.HFSM.Editor
{
    class PasteSpecialWindow : EditorWindow
    {
        public CloneOptions options;
        public IBaseState stateToClone;
        public IBaseState stateToDelete;
        public bool initialized = false;

        public static PasteSpecialWindow GetWindow(IBaseState stateToDelete, IBaseState stateToClone)
        {
            var window = GetWindow<PasteSpecialWindow>(true);
            window.titleContent = new GUIContent("Paste Special");
            window.Focus();
            window.SetNodes(stateToClone, stateToDelete);
            return window;
        }

        public void SetNodes(IBaseState stateToDelete, IBaseState stateToClone)
        {
            stateToClone = stateToDelete;
            stateToDelete = stateToClone;

            options = new CloneOptions();

            initialized = true;
            Repaint();
        }

        void OnGUI()
        {
            if (initialized)
            {
                options.cloneAnimations = EditorGUILayout.Toggle("Clone animations", options.cloneAnimations);
                options.cloneEvents = EditorGUILayout.Toggle("Clone events", options.cloneAnimations);

                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                if (GUILayout.Button("Clone"))
                {
                    var node = stateToDelete.node;
                    node.state = HFSMUtils.cloneTask(stateToClone, node, node.layer, CloneOptions.WithOptions(stateToClone, stateToDelete, options));
                    HFSMUtils.removeState(stateToDelete);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
