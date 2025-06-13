using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Behaviours.HFSM.Editor
{
    public class HFSMGUI
    {
        public static void DrawLayer(Layer layer)
        {
            if (layer)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                // DRAW LAYER NAME
                EditorGUI.BeginChangeCheck();
                layer.name = EditorGUILayout.TextField("Layer name", layer.name);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(layer);
                }

                // DRAW LAYER ENABLED
                EditorGUI.BeginChangeCheck();
                layer.enabled = EditorGUILayout.Toggle("Layer enabled", layer.enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(layer);
                }

                GUILayout.EndVertical();
            }
        }

        public static void DrawTransition(Layer layer, Node node, Transition transition)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                int idxToDelete = -1;
                for (int i = 0; i < transition.conditions.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        ValueGUI.DrawConditionValues(layer, transition.conditions[i]);

                        if (i > 0)
                        {
                            EditorGUI.BeginChangeCheck();
                            transition.conditions[i].nextOperand = (Operand)EditorGUILayout.EnumPopup(transition.conditions[i].nextOperand, GUILayout.Width(40));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorUtility.SetDirty(transition.conditions[i]);
                            }
                        }
                        else
                        {
                            GUILayout.Label("", GUILayout.Width(40));
                        }
                       

                        if (GUILayout.Button(new GUIContent("-"), GUILayout.Width(20)))
                        {
                            idxToDelete = i;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (idxToDelete != -1)
                {
                    HFSMUtils.removeCondition(transition.conditions[idxToDelete], transition);
                }

                if (GUILayout.Button("Add Condition"))
                {
                    HFSMUtils.createCondition(transition);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static float _durationPropValue;

        public static void DrawState(Layer layer, IComposedState group, Node node)
        {
            // DRAW STATE NAME
            EditorGUI.BeginChangeCheck();
            node.title = EditorGUILayout.TextField("Node name", node.title);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(node);
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                if (node.transitions.Count > 0)
                {
                    // PRECALCULATE STATES
                    List<string> stateNames = new List<string>();
                    if (group)
                    {
                        group.nodes.ForEach(delegate (Node s)
                        {
                            stateNames.Add(s.title);
                        });
                    }

                    int idxToDelete = -1;
                    int idxToMoveUp = -1;
                    int idxToMoveDown = -1;

                    for (int i = 0; i < node.transitions.Count; ++i)
                    {
                        node.transitions[i]._editorFoldout = EditorGUILayout.Foldout(node.transitions[i]._editorFoldout, node.transitions[i].node != null ? node.transitions[i].node.title : "NONE");
                        if (node.transitions[i]._editorFoldout)
                        {
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    ValueGUI.BeginBackColor(Color.cyan);
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        int oldStateIndex = group.FindIdxByNode(node.transitions[i].node);
                                        int newStateIndex = EditorGUILayout.Popup("Transition", oldStateIndex, stateNames.ToArray());
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            if (newStateIndex != -1)
                                                node.transitions[i].node = group.FindNodeByIdx(newStateIndex);
                                            else
                                                node.transitions[i].node = null;

                                            EditorUtility.SetDirty(node.transitions[i]);
                                        }
                                    }
                                    ValueGUI.EndBackColor();

                                    EditorGUI.BeginChangeCheck();
                                    node.transitions[i].weight = EditorGUILayout.FloatField(node.transitions[i].weight, GUILayout.Width(20));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorUtility.SetDirty(node.transitions[i]);
                                    }

                                    if (GUILayout.Button(new GUIContent("U"), EditorStyles.miniButton, GUILayout.Width(17)))
                                    {
                                        idxToMoveUp = i;
                                    }

                                    if (GUILayout.Button(new GUIContent("D"), EditorStyles.miniButton, GUILayout.Width(17)))
                                    {
                                        idxToMoveDown = i;
                                    }

                                    if (GUILayout.Button(new GUIContent("-"), EditorStyles.miniButton, GUILayout.Width(15)))
                                    {
                                        idxToDelete = i;
                                    }                                    
                                }
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginChangeCheck();
                                    node.transitions[i].hasExitTime = EditorGUILayout.Toggle("Has exit time", node.transitions[i].hasExitTime);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorUtility.SetDirty(node.transitions[i]);
                                    }

                                    if (node.transitions[i].hasExitTime)
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        node.transitions[i].exitTime = EditorGUILayout.FloatField(node.transitions[i].exitTime);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            EditorUtility.SetDirty(node.transitions[i]);
                                        }
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                DrawTransition(layer, node, node.transitions[i]);
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }

                    // DELETE SELECTED TRANSITION
                    if (idxToDelete != -1)
                    {
                        HFSMUtils.removeTransition(node.transitions[idxToDelete], node);
                    }

                    if (idxToMoveUp != -1)
                    {
                        if (idxToMoveUp > 0)
                        {
                            var temp = node.transitions[idxToMoveUp-1];
                            node.transitions[idxToMoveUp-1] = node.transitions[idxToMoveUp];
                            node.transitions[idxToMoveUp] = temp;

                            EditorUtility.SetDirty(node);
                        }
                    }

                    if (idxToMoveDown != -1)
                    {
                        if (idxToMoveDown < (node.transitions.Count-1))
                        {
                            var temp = node.transitions[idxToMoveDown + 1];
                            node.transitions[idxToMoveDown + 1] = node.transitions[idxToMoveDown];
                            node.transitions[idxToMoveDown] = temp;

                            EditorUtility.SetDirty(node);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This node has no transitions!", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
        }


        public static Color colorStateDefault => new Color(1.0f, 0.5f, 0.25f, 1);
        public static Color colorStateAny => new Color(0.5f, 0.25f, 0.5f, 1);
        public static Color colorStateExit => Color.cyan;
        public static Color colorGroupNormal => new Color(48/255f, 48/255f, 48/255f);
        public static Color colorStateNormal => new Color(38/255f, 38/255f, 38/255f);

        public static Color GetNodeBackgroundColor(Node node, bool running, bool selected)
        {
            Color color = colorStateNormal;

            if (node)
            {
                if (node.state is IComposedState)
                {
                    color = colorGroupNormal;
                }

                if (node.parent)
                {
                    IComposedState parent = node.parent.state as IComposedState;
                    if (parent)
                    {
                        int idx = parent.FindIdxByNode(node);

                        if (idx == parent.anyNodeIndex)
                        {
                            color = colorStateAny;
                        }

                        if (idx == parent.defaultNodeIndex)
                        {
                            color = colorStateDefault;
                        }

                        if (idx == parent.exitNodeIndex)
                        {
                            color = colorStateExit;
                        }

                        if (running)
                            color = new Color(1, 0, 0, 1);
                    }
                }
            }

            if (selected)
            {
                color += new Color(0.5f, 0.5f, 0.5f, 0);
            }

            return color;
        }
    }
}