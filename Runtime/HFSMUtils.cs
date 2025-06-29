using System;
using System.Collections.Generic;
using System.Linq;
using Behaviours.HFSM;
using UnityEngine;

namespace Behaviours
{
    public class HFSMUtils
    {
        static void checkAddToAsset(ScriptableObject target, string assetPath, UnityEngine.ScriptableObject parent = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath) == false)
            {
                if (parent)
                {
                    Debug.LogFormat("Adding {0}:{1} to {2}", target.GetType().Name, target.name, assetPath);

                    UnityEditor.AssetDatabase.AddObjectToAsset(target, assetPath);

                    UnityEditor.EditorUtility.SetDirty(parent);
                }
                else
                {
                    Debug.LogFormat("Creating {0}:{1} at {2}", target.GetType().Name, target.name, assetPath);

                    UnityEditor.AssetDatabase.CreateAsset(target, assetPath);
                    // UnityEditor.AssetDatabase.SaveAssets();
                    // UnityEditor.AssetDatabase.Refresh();
                }
            }
#endif
        }

        public static HFSM.Node Clone(HFSM.Node original, HFSM.IComposedState parent, string assetPath)
        {
            Debug.AssertFormat(original, "Original node {0} is NULL", parent.name);

            HFSM.Node node = UnityEngine.Object.Instantiate<HFSM.Node>(original);
            {
                node.title = original.title;
                node.layer = parent.node.layer;
                node.parent = parent.node;

                if (original.state)
                {
                    node.state = Clone(original.state, node, assetPath);
                }

                checkAddToAsset(node, assetPath, parent);
            }

            return node;
        }

        public static HFSM.Node Clone(HFSM.Node original, HFSM.Layer parent, string assetPath)
        {
            HFSM.Node node = UnityEngine.Object.Instantiate<HFSM.Node>(original);
            {
                node.title = original.title;
                node.layer = parent;

                if (original.state)
                {
                    node.state = Clone(original.state, node, assetPath);
                }

                parent.root = node;

                checkAddToAsset(node, assetPath, parent);
            }
            return node;
        }

        public static HFSM.IBaseState Clone(HFSM.IBaseState original, HFSM.Node node, string assetPath)
        {
            Debug.AssertFormat(original, "Original state {0} is NULL", node.title);

            HFSM.IBaseState state = UnityEngine.Object.Instantiate<HFSM.IBaseState>(original);
            {
                state.name = original.name;
                state.node = node;

                if (state is HFSM.IComposedState)
                {
                    HFSM.IComposedState originalComposite = original as HFSM.IComposedState;

                    // CLONE NODES
                    HFSM.IComposedState composite = state as HFSM.IComposedState;
                    for (int i = 0; i < composite.nodes.Count; ++i)
                    {
                        composite.nodes[i] = Clone(originalComposite.nodes[i], composite, assetPath);
                    }

                    // CLONE TRANSITIONS
                    for (int i = 0; i < composite.nodes.Count; ++i)
                    {
                        Debug.AssertFormat(composite.nodes[i], "Node {0} is NULL", i);

                        for (int j = 0; j < composite.nodes[i].transitions.Count; ++j)
                        {
                            Debug.AssertFormat(composite.nodes[i].transitions[j] != null, "Transition {0} of node {1} is NULL", j, composite.nodes[i].title);

                            int transitionNodeIdx = originalComposite.FindIdxByNode(originalComposite.nodes[i].transitions[j].node);
                            Debug.AssertFormat(transitionNodeIdx != -1, "Transition not found from {0} to {1}", composite.nodes[i].title, composite.nodes[i].transitions[j].node.title);
                            composite.nodes[i].transitions[j] = Clone(originalComposite.nodes[i].transitions[j], composite.nodes[transitionNodeIdx], assetPath);
                        }
                    }
                }

                checkAddToAsset(state, assetPath, node);
            }

            return state;
        }

        public static HFSM.Transition Clone(HFSM.Transition original, HFSM.Node node, string assetPath)
        {
            HFSM.Transition transition = UnityEngine.Object.Instantiate<HFSM.Transition>(original);
            transition.name = "Copy of " + original.name;
            transition.node = node;

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath) == false)
                UnityEditor.AssetDatabase.AddObjectToAsset(transition, assetPath);
#endif

            for (int i = 0; i < transition.conditions.Count; ++i)
            {
                transition.conditions[i] = VarsUtils.Clone(original.conditions[i], assetPath);

                // UPDATE CONDITION VALUE
                int valueIndex = original.node.layer.values.IndexOf(original.conditions[i].value);
                transition.conditions[i].value = transition.node.layer.values[valueIndex];                

#if UNITY_EDITOR
                if (string.IsNullOrEmpty(assetPath) == false)
                    UnityEditor.EditorUtility.SetDirty(transition.conditions[i]);
#endif
            }
            return transition;
        }
    }
}
