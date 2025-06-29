using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Behaviours.HFSM.Runtime;
using Newtonsoft.Json.Linq;

namespace Behaviours.HFSM.Editor
{
    public class HFSMWindow : GenericWindow<Node>
    {
        private const string PasteSpacialType_AddMissingNode = "Add Missing Node";
        private const string PasteSpacialType_AddMissingState = "Add Missing State";
        private const string PasteSpacialType_ChangeStateType = "Change State Type";
        private const string PasteSpacialType_ChangeStateProperty = "Change State Property";

        public Layer layer;
        public IComposedState currentGroup;

        private Animator animator;

        private Node nodeWithTransition;
        private Node nodeBuffer;
        private IService serviceBuffer;

        private int doMyWindowTab = 0;
        private UnityEditor.Editor selectedStateEditor = null;
        private List<UnityEditor.Editor> selectedServiceEditors = null;
        private int selectedSpideStateData;
        private bool needsUpdate = false;
        private Vector2 propertiesWindowGUIPos;
        private List<MissingStatesNodeData> _missingStatesNodeDatas;
        private Dictionary<object, bool> _missingStatesNodeDatasDict;
        private Node _missingStatesNode1;
        private Node _missingStatesNode2;
        public Animator[] availableAnimators = null;

        public class AnimatableStateData
        {
            public IAnimatableState animatableState;
            public string name;
            public string value;
        }

        public class SetScriptInfo
        {
            public System.Type type;
            public Node node;
        }
        
        public class MissingStatesNodeData
        {
            public Node targetParentNode;
            public Node sourceNode;

            public Dictionary<string, SerializedProperty[]> changedProperties;
            public List<Transition> addedTransitions;

            public string type;
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/HFSM Window")]
        private static void Init()
        {
            GetWindow(typeof(HFSMWindow), false);
        }

        private void OnLostFocus()
        {
            SaveLastGroup();
        }

        private void RestoreLastGroup()
        {
            if (!layer) 
                return;
            
            var layerName = layer.name.Replace(" ", "_");
            var key = $"LAST_CURRENT_GROUP_{layerName}";
            if (!EditorPrefs.HasKey(key)) 
                return;
                
            var path = EditorPrefs.GetString(key);
            var parts = path.Split(new[] { '/' });
            var temp = layer.root;

            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!temp) 
                    continue;
                        
                var composedState = temp.state as IComposedState;
                if (!composedState)
                    return;
                
                temp = composedState.FindNodeByTitle(part);
            }

            if (!temp)
                return;
            
            currentGroup = temp.state as IComposedState;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DestroyStateEditor();
            DestroyServicesEditors();
            SaveLastGroup();
        }


        #region GenericWindow Implementation
        
        public override bool RequiresUpdate => base.RequiresUpdate || needsUpdate;
        
        public override void OnEnable()
        {
            base.OnEnable();

            OnSelectionChange();
        }
        
        public override int AddEmptyItem(Vector2 position)
        {
            if (currentGroup)
            {
                Node childNode = HFSMUtils.createNode(currentGroup);
                {
                    childNode.rect.position = position;
                    EditorUtility.SetDirty(childNode);
                }

                int idx = currentGroup.nodes.Count - 1;

                setSelectedItem(idx);

                Repaint();

                return idx;
            }
            return -1;
        }

        public override void RemoveItemAtIndex(int index)
        {
            if (currentGroup)
            {
                Node node = currentGroup.nodes[index];
                HFSMUtils.RemoveNode(node, currentGroup);

                EditorUtility.SetDirty(currentGroup);
            }

            base.RemoveItemAtIndex(index);
        }

        public override Node GetItem(int index)
        {
            if (currentGroup)
            {
                return currentGroup.nodes[index];
            }
            return null;
        }

        public override int GetItemsCount()
        {
            if (currentGroup)
            {
                return currentGroup.nodes.Count;
            }
            return 0;
        }

        public override string GetItemName(int index)
        {
            if (currentGroup)
            {
                return currentGroup.name;
            }
            return null;
        }

        public override Rect GetItemRect(int index)
        {
            if (currentGroup)
            {
                if (currentGroup.nodes[index])
                {
                    return currentGroup.nodes[index].rect;
                }
            }
            return Rect.MinMaxRect(0, 0, 1, 1);
        }

        public override void SetItemRect(int index, Rect newRect)
        {
            if (currentGroup)
            {
                currentGroup.nodes[index].rect = newRect;
                EditorUtility.SetDirty(currentGroup.nodes[index]);
            }
        }       

        public override void DrawItem(int index)
        {
            if (!currentGroup) 
                return;
            
            var node = GetItem(index);
            if (!node)
            {
                DisplayNodeIsNull(index);
                return;
            }

            var rn = FindRuntimeDataForNode(node);
            var running = rn is { isRunning: true };
            var selected = isSelected(index);
            if (selected)
            {
                DrawSelected();
            }

            GUI.BeginGroup(node.rect);
            {
                Color backColor = HFSMGUI.GetNodeBackgroundColor(node, running, false);
                if (node.state)
                {
                    DrawWithState(backColor);
                }
                else
                {
                    DrawEmpty(backColor);
                }

                if (currentGroup.nodes.IndexOf(node) == currentGroup.anyNodeIndex)
                {
                    GUI.Label(new Rect(5, -2, node.rect.width - 15, 30), "ANY", LabelStateParamStyle);
                }
                else if (currentGroup.nodes.IndexOf(node) == currentGroup.exitNodeIndex)
                {
                    GUI.Label(new Rect(5, -2, node.rect.width - 15, 30), "EXIT", LabelStateParamStyle);
                }
                else if (currentGroup.nodes.IndexOf(node) == currentGroup.defaultNodeIndex)
                {
                    GUI.Label(new Rect(5, -2, node.rect.width - 15, 30), "DEFAULT", LabelStateParamStyle);
                }
            }
            GUI.EndGroup();

            if (running)
            {
                EditorBehaviourGUI.FillRectangle(new Rect(node.rect.x + 1, node.rect.center.y, (node.rect.width-2) * rn.runtimeStateData.ratio, 3), new Color(1, 0, 0, 0.25f));
            }

            void DrawSelected()
            {
                float b = 3;
                var sel = new Rect(node.rect.position - Vector2.one * b, node.rect.size + Vector2.one * (b * 2));
                GUI.BeginGroup(sel);
                {
                    EditorBehaviourGUI.FillRectangle(new Rect(0, 0, sel.width, sel.height), new Color(0, 0.5f, 1));
                    GUI.Label(new Rect(0, 0, node.rect.width, node.rect.height), " ", LabelStateStyle);
                }
                GUI.Label(new Rect(0, 0, sel.width, sel.height), " ", LabelStateStyle);

                GUI.EndGroup();
            }

            void DrawWithState(Color backColor)
            {
                EditorBehaviourGUI.FillRectangle(new Rect(0, 0, node.rect.width, node.rect.height), HFSMGUI.colorStateNormal);
                GUI.Label(
                    new Rect(0, 0, node.rect.width, node.rect.height),
                    new GUIContent(node.title, EditorBehaviourGUI.GetTextureByFSMStateType(node.state.GetType())),
                    LabelStateStyle);
                
                DrawLine(backColor);

                GUI.Label(
                    new Rect(0, node.rect.height - 30, node.rect.width, 30), node.state.GetType().Name,
                    EditorBehaviourGUI.labelStateScript);
            }

            void DrawEmpty(Color backColor)
            {
                EditorBehaviourGUI.FillRectangle(new Rect(0, 0, node.rect.width, node.rect.height), HFSMGUI.colorStateNormal);
                GUI.Label(
                    new Rect(0, 0, node.rect.width, node.rect.height), 
                    new GUIContent(node.title),
                    LabelEmptyStateStyle);

                DrawLine(backColor);
                
                EditorBehaviourGUI.EndTint();
            }

            void DrawLine(Color backColor)
            {
                EditorBehaviourGUI.FillRectangle(new Rect(0, 0, node.rect.width, 3), backColor);
            }
        }

        public override string GetWindowTitle()
        {
            return "HFSM";
        }

        public override string GetViewTitle()
        {
            if (!currentGroup) 
                return "";
            
            if (currentGroup.node == layer.root)
                return layer.name;

            if (currentGroup.node)
                return currentGroup.node.getSimplifiedPath();

            return currentGroup.name;
        }

        public override void DoPropertiesWindowGUI(int wndId)
        {
            propertiesWindowGUIPos = EditorGUILayout.BeginScrollView(propertiesWindowGUIPos, GUILayout.Height(windowRect.height - 45));
            {
                if (!layer)
                {
                    EditorBehaviourGUI.BeginEnable(!Application.isPlaying);
                    {
                        EditorGUILayout.HelpBox("Please select an existing layer!", MessageType.Info);

                        Layer newLayer = EditorGUILayout.ObjectField("Layer", layer, typeof(Layer), false) as Layer;
                        setLayer(newLayer);

                        EditorGUILayout.HelpBox("You can also create a new layer!", MessageType.Info);

                        if (GUILayout.Button("Create New Layer"))
                        {
                            string assetPath = EditorUtility.SaveFilePanelInProject("Create new HFSM Layer", "New HFSM Layer", "asset", "");
                            if (string.IsNullOrEmpty(assetPath) == false)
                            {
                                newLayer = HFSMUtils.createLayer(assetPath);
                                setLayer(newLayer);
                            }
                        }
                    }
                    EditorBehaviourGUI.EndEnable();
                }
                else
                {
                    if (availableAnimators != null)
                    {
                        var names = new string[availableAnimators.Length];
                        for (int i = 0; i < names.Length; i++)
                        {
                            names[i] = availableAnimators[i].GetType().Name;
                        }

                        int idx = ArrayUtility.IndexOf(availableAnimators, animator);
                        int sel = EditorGUILayout.Popup("Animators", idx, names);
                        if (sel != idx)
                        {
                            SetAnimator(availableAnimators[sel]);
                            SaveAnimatorIndex();
                        }
                    }

                    HFSMGUI.DrawLayer(layer);

                    var tabs = new [] { "Layer", "Trans", "State", "Anima" };

                    if (nodeBuffer && currentGroup && currentGroup.nodes != null && currentGroup.nodes.Count > selectedFirstIndex && selectedFirstIndex >= 0 && getSelectedItems().Count > 0)
                    {
                        ArrayUtility.Add(ref tabs, "Buffer");
                    }

                    doMyWindowTab = GUILayout.Toolbar(doMyWindowTab, tabs);
                    switch (doMyWindowTab)
                    {
                        case 0:
                        {
                            EditorGUILayout.BeginVertical();

                            if (GUILayout.Button("Export binary..."))
                            {
                                string assetPath = EditorUtility.SaveFilePanel("Save binary...", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), this.layer.name, "hfsm");
                                if (string.IsNullOrEmpty(assetPath) == false)
                                {
                                    using (BinaryWriter writer = new BinaryWriter(File.Open(assetPath, FileMode.Create)))
                                    {
                                        this.layer.Write(writer);
                                    }
                                }
                            }

                            if (animator && animator.runtimeLayer != null)
                            {
                                ValueGUI.DrawRuntimeValues("Vars", animator.runtimeLayer.runtimeValues);
                            }
                            else
                            {
                                ValueGUI.DrawLayerValues("Vars", layer);
                            }

                            EditorGUILayout.EndVertical();
                            break;
                        }
                        case 1:
                        {
                            if (selectedFirstIndex != -1 && selectedFirstIndex < currentGroup.nodes.Count)
                            {
                                GUILayout.Space(10);
                                HFSMGUI.DrawState(layer, currentGroup, currentGroup.nodes[selectedFirstIndex]);
                            }

                            break;
                        }
                        case 2:
                        {
                            if (getSelectedItems().Count == 1)
                            {
                                int nodeIndex = getSelectedItems()[0];
                                var node = currentGroup.nodes[nodeIndex];

                                EditorGUILayout.BeginVertical();
                                {
                                    if (selectedStateEditor != null)
                                    {
                                        DrawStateEditor(node);
                                    }

                                    if (selectedServiceEditors != null)
                                    {
                                        DrawServicesEditor(node);
                                    }

                                    DrawServiceAddButtons(node);

                                    GUILayout.Space(5);
                                }
                                EditorGUILayout.EndVertical();
                            }

                            break;
                        }
                        case 3 when !animator:
                            EditorGUILayout.HelpBox("Couldn't find an animator!", MessageType.Warning);
                            break;
                        case 3:
                        {
                            var states = new List<IBaseState>();
                            HFSMUtils.CollectStatesRecursive(layer.root.state as IComposedState, states);
                                            
                            var groups = new List<string>();
                            var namesToStates = new Dictionary<string, List<AnimatableStateData>>();

                            foreach (var baseState in states)
                            {
                                if (baseState is not IAnimatableState animatableState) 
                                    continue;
                                
                                var parentName = Utils.GetFullNodeName(baseState.node.parent, "/");
                                if (!namesToStates.ContainsKey(parentName))
                                {
                                    namesToStates[parentName] = new List<AnimatableStateData>();
                                    groups.Add(parentName);
                                }

                                var spineStateData = new AnimatableStateData
                                {
                                    name = baseState.node.title,
                                    value = animatableState.AnimationName,
                                    animatableState = animatableState
                                };
                                        
                                namesToStates[parentName].Add(spineStateData);
                            }

                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            selectedSpideStateData = EditorGUILayout.Popup("Select a group", selectedSpideStateData, groups.ToArray());
                            EditorGUILayout.EndVertical();

                            var idx = 0;
                            // Find IAnimatable type by name
                            var animatableType = System.AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(assembly => assembly.GetTypes())
                                .FirstOrDefault(type => type.Name == "IAnimatable");

                            if (animatableType == default)
                                // TODO: Make it dynamical tab
                                break;
                            
                            // call animator.GetComponentInChildren with the animatableType
                            var animatable = animator.GetComponentInChildren(animatableType);
                            var animationsProperty = animatableType.GetProperty("Animations");
                            var animations = animationsProperty != null 
                                ? (List<string>)animationsProperty.GetValue(animatable) 
                                : new List<string>();

                            // TODO: This is a workaround to get the animations from the animator.
                            //var animatable = animator.GetComponentInChildren<IAnimatable>();
                            //var animations = animatable is { Animations: not null } ? animatable.Animations : new List<string>();
                            foreach (var keyValuePair in namesToStates)
                            {
                                if (selectedSpideStateData != idx)
                                {
                                    idx++;
                                    continue;
                                }
                                
                                foreach (var list in keyValuePair.Value)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var oldIdx = animations.IndexOf(list.value);
                                        var newIdx = EditorGUILayout.Popup(list.name, oldIdx, animations.ToArray());
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            list.animatableState.AnimationName = newIdx != -1 ? animations[newIdx] : "";
                                            EditorUtility.SetDirty((Object)list.animatableState);
                                        }

                                        if (GUILayout.Button("*", GUILayout.Width(20)))
                                        {
                                            IComposedState cs = ((IBaseState)list.animatableState).node.parent.state as IComposedState;
                                            if (cs)
                                            {
                                                currentGroup = cs;
                                                setSelectedItem(cs.nodes.IndexOf(((IBaseState)list.animatableState).node));
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                
                                idx++;
                            }

                            break;
                        }
                        case 4:
                        {
                            var node = currentGroup.nodes[selectedFirstIndex];
                            if (_missingStatesNodeDatas != null && nodeBuffer == _missingStatesNode1 && node == _missingStatesNode2)
                            {
                                for (var i = 0; i < _missingStatesNodeDatas.Count; i++)
                                {
                                    var d = _missingStatesNodeDatas[i];

                                    {
                                        var b = true;
                                        if (_missingStatesNodeDatasDict.ContainsKey(d))
                                        {
                                            b = _missingStatesNodeDatasDict[d];
                                        }
                                        _missingStatesNodeDatasDict[d] = EditorGUILayout.ToggleLeft(d.sourceNode.title, b);

                                        EditorGUI.indentLevel++;
                                    }

                                    EditorGUI.indentLevel++;

                                    GUILayout.BeginVertical();
                                    {
                                        if (d.changedProperties != null)
                                        {
                                            foreach (var kvp in d.changedProperties)
                                            {
                                                GUILayout.BeginHorizontal();
                                                {
                                                    var b = true;
                                                    if (_missingStatesNodeDatasDict.ContainsKey(kvp.Value))
                                                    {
                                                        b = _missingStatesNodeDatasDict[kvp.Value];
                                                    }
                                                    _missingStatesNodeDatasDict[kvp.Value] = EditorGUILayout.ToggleLeft(kvp.Key, b);
                                                }
                                                GUILayout.EndHorizontal();
                                            }
                                        }
                                    }
                                    GUILayout.EndVertical();

                                    EditorGUI.indentLevel--;

                                    EditorGUI.indentLevel--;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Diff"))
                                {
                                    _missingStatesNodeDatas = new List<MissingStatesNodeData>();
                                    DetectMissingStates(nodeBuffer, currentGroup.nodes[selectedFirstIndex], ref _missingStatesNodeDatas);

                                    _missingStatesNode1 = nodeBuffer;
                                    _missingStatesNode2 = currentGroup.nodes[selectedFirstIndex];

                                    _missingStatesNodeDatasDict = new Dictionary<object, bool>();
                                }
                            }

                            break;
                        }
                    }
                }

                base.DoPropertiesWindowGUI(wndId);
            }
            EditorGUILayout.EndScrollView();
        }

        public override GenericMenu CreateMenu(int mouseDownIndex, Vector2 mousePosition)
        {
            GenericMenu menu = base.CreateMenu(mouseDownIndex, mousePosition);
            {
                if (mouseDownIndex != -1)
                {
                    Node node = GetItem(mouseDownIndex);

                    //menu.AddItem(new GUIContent("Delete state"), false, OnContextMenuItemClick_DeleteHFSMNode, node);
                    menu.AddItem(new GUIContent("Create transition..."), false, OnContextMenuItemClick_CreateTransition, node);

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Copy state"), false, OnContextMenuItemClick_CopyState, node);
                    if (nodeBuffer)
                    { 
                        menu.AddItem(new GUIContent("Paste state"), false, OnContextMenuItemClick_PasteState, node);
                        menu.AddItem(new GUIContent("Paste special/Custom..."), false, OnContextMenuItemClick_PasteStateCustom, node);
                        menu.AddItem(new GUIContent("Paste special/Without events"), false, OnContextMenuItemClick_PasteStateNoEvents, node);
                        menu.AddItem(new GUIContent("Paste special/Without animations"), false, OnContextMenuItemClick_PasteStateNoAnimations, node);
                        menu.AddItem(new GUIContent("Paste special/Without events or animations"), false, OnContextMenuItemClick_PasteStateNoEventsOrAnimations, node);
                        menu.AddItem(new GUIContent("Paste special/Show diff"), false, OnContextMenuItemClick_PasteOnlyMissingStates, node);
                        menu.AddItem(new GUIContent("Paste special/Paste only missing"), false, OnContextMenuItemClick_PasteOnlyMissingNodes, node);
                        menu.AddItem(new GUIContent("Paste special/Paste only transitions"), false, OnContextMenuItemClick_PasteOnlyTransitions, node);
                    }
                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Select asset"), false, OnContextMenuItemClick_SelectAsset, node);
                    if (node.state is IComposedState)
                    {
                        menu.AddItem(new GUIContent("Enter group"), false, OnContextMenuItemClick_EnterGroup, node);
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Export/Export As JSON..."), false, OnContextMenuItemClick_ExportJSON, node);
                        menu.AddItem(new GUIContent("Export/Export As Asset"), false, OnContextMenuItemClick_ExportAsset, node);
                        menu.AddItem(new GUIContent("Fix group parent..."), false, OnContextMenuItemClick_FixGroupParent, node);

                        menu.AddSeparator("");

                        var types = HFSMUtils.GetStatesTypes();
                            
                        for (int i = 0; i < types.Count; ++i)
                        {
                            SetScriptInfo ssi = new SetScriptInfo();
                            ssi.node = node;
                            ssi.type = types[i];
                            menu.AddItem(new GUIContent("Set script.../" + types[i].FullName.Replace(".", "/")), false, OnContextMenuItemClick_HFSMSetState, ssi);
                        }

                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Remove Script"), false, OnContextMenuItemClick_RemoveState, node);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Set as/Normal"), false, OnContextMenuItemClick_HFSMSetNormal, node);
                        menu.AddItem(new GUIContent("Set as/Default"), false, OnContextMenuItemClick_HFSMSetDefault, node);
                        menu.AddItem(new GUIContent("Set as/Any state"), false, OnContextMenuItemClick_HFSMSetAny, node);
                        menu.AddItem(new GUIContent("Set as/Exit state"), false, OnContextMenuItemClick_HFSMSetExit, node);
                        menu.AddSeparator("");

                        var types = HFSMUtils.GetStatesTypes();
                        for (int i = 0; i < types.Count; ++i)
                        {
                            SetScriptInfo ssi = new SetScriptInfo();
                            ssi.node = node;
                            ssi.type = types[i];
                            menu.AddItem(new GUIContent("Set script.../" + types[i].FullName.Replace(".", "/")), false, OnContextMenuItemClick_HFSMSetState, ssi);
                        }

                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Remove Script"), false, OnContextMenuItemClick_RemoveState, node);
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Import..."), false, OnContextMenuItemClick_ImportGroup, node);
                }
                else
                {
                    if (currentGroup && currentGroup.node.parent)
                    {
                        menu.AddItem(new GUIContent("Select asset"), false, OnContextMenuItemClick_PasteNode, currentGroup.node);
                        menu.AddItem(new GUIContent("Select asset"), false, OnContextMenuItemClick_SelectAsset, currentGroup.node);
                        menu.AddItem(new GUIContent("Exit group"), false, OnContextMenuItemClick_ExitGroup, currentGroup);
                    }
                }
            }
            return menu;
        }

        public override void OnItemMouseDown(Event evt, int index)
        {
            base.OnItemMouseDown(evt, index);

            if (currentGroup)
            {
                DestroyStateEditor();
                DestroyServicesEditors();

                if (getSelectedItems().Count == 1)
                {
                    var firstSelectedItemIndex = getSelectedItems()[0];
                    if (currentGroup.nodes[firstSelectedItemIndex].state != null)
                    {
                        CreateStateEditor(currentGroup.nodes[firstSelectedItemIndex].state);
                    }

                    if (currentGroup.nodes[firstSelectedItemIndex].services != null)
                    {
                        CreateServicesEditors(currentGroup.nodes[firstSelectedItemIndex].services);
                    }
                }

                if (nodeWithTransition)
                {
                    Node nodeToTransitionTo = GetItem(index);
                    if (nodeToTransitionTo)
                    {
                        if (nodeToTransitionTo != nodeWithTransition)
                        {
                            HFSMUtils.createTransition(nodeWithTransition, nodeToTransitionTo);

                            evt.Use();
                        }
                    }

                    nodeWithTransition = null;
                }
            }
        }

        public override void DrawBeforeItems()
        {
            base.DrawBeforeItems();

            int objectsCount = GetItemsCount();

            for (int i = objectsCount - 1; i >= 0; --i)
            {
                DrawTransitions(i);
            }

            for (int i = objectsCount - 1; i >= 0; --i)
            {
                DrawArrows(i);
            }
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();

            availableAnimators = null;

            DestroyStateEditor();

            if (Selection.activeGameObject)
            {
                var animators = Selection.activeGameObject.GetComponents<Animator>();
                if (animators.Length > 0)
                {
                    availableAnimators = animators;

                    var idx = GetSavedAnimatorIndex();
                    if (idx < 0 || idx >= availableAnimators.Length)
                    {
                        idx = 0;
                    }

                    var animator = availableAnimators[idx];
                    SetAnimator(animator);
                }                
            }
            else if (Selection.activeObject is Layer)
            {
                setLayer(layer);
            }

            RestoreLastGroup();
        }

        public override void OnPlayModeChanged(bool isPlaying)
        {
            base.OnPlayModeChanged(isPlaying);

            clearSelection();

            OnSelectionChange();
        }        


        #endregion
        
        private void CreateStateEditor(IBaseState state)
        {
            selectedStateEditor = UnityEditor.Editor.CreateEditor(state);
            needsUpdate = state is IAnimatableState;
        }

        private void DestroyStateEditor()
        {
            if (selectedStateEditor)
            {
                DestroyImmediate(selectedStateEditor);
                selectedStateEditor = null;
            }

            needsUpdate = false;
        }

        private void CreateServicesEditors(List<IService> services)
        {
            selectedServiceEditors = new List<UnityEditor.Editor>();
            foreach (var service in services)
            {
                selectedServiceEditors.Add(UnityEditor.Editor.CreateEditor(service));
            }
            needsUpdate = true;
        }

        private void DestroyServicesEditors()
        {
            if (selectedServiceEditors != null)
            {
                foreach (var editor in selectedServiceEditors)
                {
                    DestroyImmediate(editor);
                }
                selectedServiceEditors = null;
            }

            needsUpdate = false;
        }

        private void SaveLastGroup()
        {
            if (layer && currentGroup)
            {
                var path = currentGroup.getPath();
                var layerName = layer.name.Replace(" ", "_");
                var key = "LAST_CURRENT_GROUP_" + layerName;
                EditorPrefs.SetString(key, path);
            }
        }

        private int GetSavedAnimatorIndex()
        {
            var key = "HFSM_LAST_ANIMATOR_INDEX";
            var idx = EditorPrefs.GetInt(key, 0);
            return idx;
        }

        private void SaveAnimatorIndex()
        {
            if (animator)
            {
                if (availableAnimators != null)
                {
                    int idx = ArrayUtility.IndexOf(availableAnimators, animator);
                    var key = "HFSM_LAST_ANIMATOR_INDEX";
                    EditorPrefs.SetInt(key, idx);
                }
            }
        }

        private void SetAnimator(Animator animator)
        {
            this.animator = animator;
            if (this.animator)
            {
                setLayer(this.animator.layer);
            }
        }

        #region OTHER        

        private void DrawTransitions(int idx)
        {
            var node = GetItem(idx);
            if (!node) 
                return;
            
            var p0 = node.rect.center;

            var nodesCounts = new Dictionary<Node, int>();
            for (var i = 0; i < node.transitions.Count; ++i)
            {
                if (!node.transitions[i])
                {
                    node.transitions.RemoveAt(i);
                    EditorUtility.SetDirty(node);
                    return;
                }

                var transitionToNode = node.transitions[i].node;
                if (!transitionToNode) 
                    continue;
                
                nodesCounts[transitionToNode] = !nodesCounts.ContainsKey(transitionToNode) ? 
                    1 : 
                    nodesCounts[transitionToNode] += 1;
            }

            foreach (var (transitionToNode, _) in nodesCounts)
            {
                var p1 = transitionToNode.rect.center;
                var c = isSelected(idx) ? 
                    new Color(0.5f, 0.5f, 1.0f, 1) : 
                    new Color(0.375f, 0.375f, 0.375f, 0.5f);

                GetPointsFromTo(node.rect, transitionToNode.rect, out var pointA, out var handleA, out var handleB,
                    out var pointB);

                // Use the improved Bezier handles for better curves
                Handles.DrawBezier(pointA, pointB, handleA, handleB, c, null, 2);
                // EditorBehaviourGUI.DrawCurve(p0, p1, c, 3, 0.5f); // Remove or comment out the old line
            }
        }

        // Improved Bezier tangent calculation for transition lines
        private void GetPointsFromTo(Rect r1, Rect r2, out Vector3 pointA, out Vector3 handleA, out Vector3 handleB, out Vector3 pointB)
        {
            // Get the center points of the two rectangles
            Vector3 center1 = r1.center;
            Vector3 center2 = r2.center;

            // Calculate the direction vector between the two centers
            Vector3 direction = (center2 - center1).normalized;

            // Determine the closest edge point on the first rectangle (r1)
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal connection (left/right)
                if (direction.x > 0)
                    pointA = new Vector3(r1.xMax, center1.y, 0);
                else
                    pointA = new Vector3(r1.xMin, center1.y, 0);
            }
            else
            {
                // Vertical connection (top/bottom)
                if (direction.y > 0)
                    pointA = new Vector3(center1.x, r1.yMax, 0);
                else
                    pointA = new Vector3(center1.x, r1.yMin, 0);
            }

            // Determine the closest edge point on the second rectangle (r2)
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0)
                    pointB = new Vector3(r2.xMin, center2.y, 0);
                else
                    pointB = new Vector3(r2.xMax, center2.y, 0);
            }
            else
            {
                if (direction.y > 0)
                    pointB = new Vector3(center2.x, r2.yMin, 0);
                else
                    pointB = new Vector3(center2.x, r2.yMax, 0);
            }

            // Calculate tangent direction for handles
            Vector3 tangentA, tangentB;
            float tangentLength = Mathf.Max(Vector3.Distance(pointA, pointB) * 0.5f, 40f);

            // Handles extend horizontally or vertically from the node, depending on connection
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                tangentA = new Vector3(direction.x, 0, 0);
                tangentB = new Vector3(-direction.x, 0, 0);
            }
            else
            {
                tangentA = new Vector3(0, direction.y, 0);
                tangentB = new Vector3(0, -direction.y, 0);
            }

            handleA = pointA + tangentA * tangentLength;
            handleB = pointB + tangentB * tangentLength;
        }




        private void DrawArrows(int idx)
        {
            Node node = GetItem(idx);
            if (node)
            {
                Dictionary<Node, int> nodesCounts = new Dictionary<Node, int>();

                for (int i = 0; i < node.transitions.Count; ++i)
                {
                    if (node.transitions[i] == null)
                    {
                        node.transitions.RemoveAt(i);
                        EditorUtility.SetDirty(node);
                        return;
                    }

                    Node transitionToNode = node.transitions[i].node;
                    if (transitionToNode != null)
                    {
                        if (!nodesCounts.ContainsKey(transitionToNode))
                        {
                            nodesCounts[transitionToNode] = 1;
                        }
                        else
                        {
                            nodesCounts[transitionToNode] = nodesCounts[transitionToNode] + 1;
                        }
                    }
                }

                foreach (var kvp in nodesCounts)
                {
                    var transitionToNode = kvp.Key;
                    var cnt = kvp.Value;

                    Color c = Color.white * 0.375f; c.a = 0.5f;
                    if (isSelected(idx))
                        c = new Color(0.5f, 0.5f, 1.0f, 1);

                    // Use the same Bezier as the transition line
                    GetPointsFromTo(node.rect, transitionToNode.rect, out var pointA, out var handleA, out var handleB, out var pointB);

                    // Spread arrows along the curve to avoid overlap
                    float spread = 0.08f; // how far apart the arrows are (as t offset)
                    float tStart = 0.5f - (spread * (cnt - 1) / 2f);

                    for (int i = 0; i < cnt; i++)
                    {
                        float t = tStart + i * spread;
                        t = Mathf.Clamp01(t);

                        Vector3 bezierPos = CalculateCubicBezierPoint(t, pointA, handleA, handleB, pointB);
                        Vector3 bezierTangent = CalculateCubicBezierTangent(t, pointA, handleA, handleB, pointB).normalized;

                        EditorBehaviourGUI.DrawArrowOnly(
                            bezierPos,
                            bezierPos + bezierTangent * 20f,
                            c, 5, 0, 1f
                        );
                    }
                }
            }
        }

        // Utility for Bezier point
        private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        // Utility for Bezier tangent (direction)
        private Vector3 CalculateCubicBezierTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 tangent =
                3 * uu * (p1 - p0) +
                6 * u * t * (p2 - p1) +
                3 * tt * (p3 - p2);

            return tangent;
        }




        private void DrawServiceAddButtons(Node node)
        {
            // CHECK ADD
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (serviceBuffer)
                {
                    if (GUILayout.Button(EditorIcons.IconPaste, EditorStyles.miniButton))
                    {
                        var newService = HFSMUtils.cloneService(serviceBuffer, node, node.layer);
                        node.services.Add(newService);
                        EditorUtility.SetDirty(node);

                        Repaint();
                    }
                }

                if (GUILayout.Button(EditorIcons.IconAdd, EditorStyles.miniButton))
                {
                    GenericMenu menu = new GenericMenu();
                    {
                        System.Type baseType = typeof(IService);
                        System.Type[] types = System.Reflection.Assembly.GetAssembly(baseType).GetTypes();
                        for (int i = 0; i < types.Length; ++i)
                        {
                            if (types[i].IsSubclassOf(baseType))
                            {
                                if (types[i] != typeof(IService))
                                {
                                    SetScriptInfo ssi = new SetScriptInfo();
                                    ssi.node = node;
                                    ssi.type = types[i];

                                    menu.AddItem(new GUIContent(types[i].FullName.Replace(".", "/")), false, data =>
                                    {
                                        var scriptInfo = (SetScriptInfo)data;
                                        Editor.HFSMUtils.AddService(scriptInfo.node, scriptInfo.type);
                                        DestroyServicesEditors();
                                        CreateServicesEditors(node.services);
                                    }, ssi);
                                }
                            }
                        }

                    }
                    menu.ShowAsContext();
                }

                GUILayout.Space(5);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServicesEditor(Node node)
        {
            if (selectedServiceEditors.Count != node.services.Count)
            {
                DestroyServicesEditors();
                CreateServicesEditors(node.services);
                needsUpdate = true;
            }

            int indexToDelete = -1;

            for (int i = 0; i < selectedServiceEditors.Count; i++)
            {
                var editor = selectedServiceEditors[i];
                var service = node.services[i];

                // SERVICE HEADER
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    bool oldFoldout = service._editorOnlyExpanded;
                    service._editorOnlyExpanded = EditorGUILayout.Foldout(oldFoldout, service.GetType().Name);
                    if (service._editorOnlyExpanded != oldFoldout)
                    {
                        EditorUtility.SetDirty(service);
                    }
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(EditorIcons.IconDublicate, EditorStyles.miniButton))
                    {
                        serviceBuffer = service;
                    }

                    if (GUILayout.Button(EditorIcons.IconDelete, EditorStyles.miniButton))
                    {
                        indexToDelete = i;
                    }                    
                }
                EditorGUILayout.EndHorizontal();

                // SERVICE DATA
                if (service._editorOnlyExpanded)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        editor.OnInspectorGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // CHECK DELETE
            if (indexToDelete != -1)
            {
                Editor.HFSMUtils.DeleteService(node, node.services[indexToDelete]);
                DestroyServicesEditors();
                CreateServicesEditors(node.services);

                needsUpdate = true;
            }
        }

        private void DrawStateEditor(Node node)
        {
            if (!node.state)
                return;
            
            bool didDeleteState = false;

            // STATE HEADER
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                bool oldFoldout = node.state._editorOnlyExpanded;
                node.state._editorOnlyExpanded = EditorGUILayout.Foldout(oldFoldout, node.state.GetType().Name);
                if (node.state._editorOnlyExpanded != oldFoldout)
                {
                    EditorUtility.SetDirty(node.state);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorIcons.IconDelete, EditorStyles.miniButton))
                {
                    didDeleteState = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            // STATE DATA
            if (node.state._editorOnlyExpanded)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    selectedStateEditor.OnInspectorGUI();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
            }

            // CHECK DELETE
            if (didDeleteState)
            {
                Editor.HFSMUtils.removeState(node.state);
                DestroyStateEditor();

                needsUpdate = true;
            }
        }

        public bool CheckNodeIsRunning(Node node)
        {
            Runtime.RuntimeNodeData rn = FindRuntimeDataForNode(node);
            if (rn != null && rn.associatedNode != null)
            {
                return rn.isRunning;
            }

            return false;
        }

        private void DisplayNodeIsNull(int nodeIndex)
        {
            if (EditorUtility.DisplayDialog("Your graph has encounterred a structure corruption!", string.Format("Item at {0} was null. Would you like to remove it?", nodeIndex), "Yes", "No"))
            {
                currentGroup.nodes.RemoveAt(nodeIndex);
                Repaint();
            }
        }

        private Runtime.RuntimeNodeData FindRuntimeDataForNode(Runtime.RuntimeGroupData group, Node node)
        {
            if (group != null)
            {
                if (group.associatedNode == node)
                {
                    return group;
                }

                for (int i = 0; i < group.nodes.Count; ++i)
                {
                    Runtime.RuntimeNodeData rtemp = group.nodes[i];
                    if (rtemp != null)
                    {
                        if (rtemp is Runtime.RuntimeGroupData)
                        {
                            Runtime.RuntimeNodeData n = FindRuntimeDataForNode(rtemp as Runtime.RuntimeGroupData, node);
                            if (n != null)
                                return n;
                        }
                        else
                        {
                            if (rtemp.associatedNode == node)
                                return rtemp;
                        }
                    }                    
                }
            }
            return null;
        }

        private Runtime.RuntimeNodeData FindRuntimeDataForNode(Node node)
        {
            if (animator && animator.runtimeLayer!=null && Application.isPlaying)
            {
                return FindRuntimeDataForNode(animator.runtimeLayer.runtimeRootGroupData, node);
            }
            return null;
        }

        #endregion

        #region CONTEXT MENU EVENTS        

        private void OnContextMenuItemClick_HFSMSetState(object userData)
        {
            SetScriptInfo ssi = userData as SetScriptInfo;

            HFSMUtils.createState(ssi.type, ssi.node);

            Repaint();
        }

        private void OnContextMenuItemClick_RemoveState(object userData)
        {
            Node state = userData as Node;

            if (state.state != null)
            {
                DestroyImmediate(state.state, true);
            }

            state.state = null;

            UnityEditor.EditorUtility.SetDirty(state);

            if (currentGroup)
            {
                UnityEditor.EditorUtility.SetDirty(currentGroup);
            }
            else
            {
                UnityEditor.EditorUtility.SetDirty(layer);
            }

            Repaint();
        }

        private void OnContextMenuItemClick_CreateTransition(object userData)
        {
            nodeWithTransition = userData as Node;
        }

        private void OnContextMenuItemClick_NewHFSMNode(object userData)
        {
            Layer layer = this.layer as Layer;

            Vector2 mousePos = (Vector2)userData;

            AddEmptyItem(mousePos);
        }

        private void OnContextMenuItemClick_ExitGroup(object userData)
        {
            IComposedState state = userData as IComposedState;

            if (state && state.node && state.node.parent && state.node.parent.state is IComposedState)
            {
                currentGroup = state.node.parent.state as IComposedState;
            }

            Repaint();
        }

        private void OnContextMenuItemClick_NewHFSMGroup(object userData)
        {
            Node node = userData as Node;

            HFSMUtils.createState(typeof(IComposedState), node);
        }

        private void OnContextMenuItemClick_DeleteHFSMNode(object userData)
        {
            Node node = userData as Node;

            HFSMUtils.RemoveNode(node, currentGroup);
        }

        private void OnContextMenuItemClick_EnterGroup(object userData)
        {
            Node node = userData as Node;

            IComposedState newGroup = node.state as IComposedState;

            if (newGroup)
            {
                IComposedState oldGroup = currentGroup;

                OnGroupEnterred(oldGroup, newGroup);

                Repaint();
            }
        }

        private void OnContextMenuItemClick_ExportJSON(object userData)
        {
            Node node = userData as Node;

            IComposedState group = node.state as IComposedState;

            if (group)
            {
                string filePath = EditorUtility.SaveFilePanel("Enter the group file...", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), node.title, "json");
                if (string.IsNullOrEmpty(filePath) == false)
                {
                    JObject jc = HFSMUtils.exportGroupToJSON(group);
                    string json = jc.ToString();
                    System.IO.File.WriteAllText(filePath, json);
                }
            }
        }

        private void OnContextMenuItemClick_ExportAsset(object userData)
        {
            Node node = userData as Node;

            IComposedState group = node.state as IComposedState;
            if (group)
            {
                string filePath = EditorUtility.SaveFilePanelInProject("Enter the group file...", node.title, "asset", "Choose where to save");
                if (string.IsNullOrEmpty(filePath) == false)
                {
                    var newLayer = HFSMUtils.createLayer(filePath, false);
                    newLayer.name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    newLayer.values = ValueUtils.cloneValues(node.layer.values, newLayer);
                    EditorUtility.SetDirty(newLayer);                    

                    var newState = HFSMUtils.cloneTask(group, newLayer.root, newLayer, new CloneOptions() { cloneAnimations = true, cloneEvents = true });

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = layer;
                }
            }
        }

        private void OnContextMenuItemClick_FixGroupParent(object userData)
        {
            Node node = userData as Node;

            IComposedState group = node.state as IComposedState;

            if (group)
            {
                if (group.node == null)
                {
                    group.node = node;
                    EditorUtility.SetDirty(group);
                    EditorUtility.DisplayDialog("A group found with no parent!", "Group parent fixed!", "Ok");
                }
            }
        }

        private void OnContextMenuItemClick_ImportGroup(object userData)
        {
            Node node = userData as Node;
            if (node)
            {
                string filePath = EditorUtility.OpenFilePanel("Enter the group file...", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "json");
                if (string.IsNullOrEmpty(filePath) == false)
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    JObject jc = JObject.Parse(json);

                    HFSMUtils.removeState(node.state);

                    IComposedState group = HFSMUtils.createState(typeof(IComposedState), node, false) as IComposedState;
                    {
                        HFSMUtils.importGroupFromJSON(node, group, jc);
                    }
                }
            }
        }

        private void OnGroupEnterred(IComposedState oldGroup, IComposedState newGroup)
        {
            if (newGroup)
            {
                clearSelection();

                currentGroup = newGroup;
            }
        }

        private void OnContextMenuItemClick_HFSMSetDefault(object userData)
        {
            Layer layer = this.layer as Layer;

            Node state = userData as Node;

            if (currentGroup)
            {
                currentGroup.defaultNodeIndex = currentGroup.FindIdxByNode(state);
                EditorUtility.SetDirty(currentGroup);
            }

            Repaint();
        }

        private void OnContextMenuItemClick_HFSMSetNormal(object userData)
        {
            Node state = userData as Node;

            if (currentGroup)
            {
                int index = currentGroup.FindIdxByNode(state);

                if (currentGroup.defaultNodeIndex == index)
                    currentGroup.defaultNodeIndex = -1;

                if (currentGroup.anyNodeIndex == index)
                    currentGroup.anyNodeIndex = -1;

                if (currentGroup.exitNodeIndex == index)
                    currentGroup.exitNodeIndex = -1;

                EditorUtility.SetDirty(currentGroup);
            }

            Repaint();
        }

        private void OnContextMenuItemClick_SelectAsset(object userData)
        {
            Node node = userData as Node;

            Selection.activeObject = node;
        }

        private void OnContextMenuItemClick_CopyState(object userData)
        {
            nodeBuffer = userData as Node;

            Repaint();
        }

        private void OnContextMenuItemClick_PasteOnlyMissingStates(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;
                IBaseState stateToClone = nodeBuffer.state;

                MultiColumnWindow.GetWindow(stateToClone.node, stateToDelete.node);
            }
        }

        private void OnContextMenuItemClick_PasteOnlyTransitions(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                foreach (var t in node.transitions)
                {
                    Debug.Log("Removing transition...");
                    HFSMUtils.removeTransition(t, node);
                }

                foreach (var t in nodeBuffer.transitions)
                {
                    if (t.node)
                    {
                        var targetNodeTitle = t.node.title;

                        if (!targetNodeTitle.Equals(node.title))
                        {
                            var targetNode = HFSMUtils.FindNodeByTitle(node.parent, targetNodeTitle);

                            if (targetNode)
                            {
                                var newTransition = HFSMUtils.cloneTransition(t, targetNode, node.layer, CloneOptions.WithReplacementData(t, targetNode, true, true));
                                node.transitions.Add(newTransition);
                            }
                            else
                            {
                                Debug.LogWarningFormat("Couldn't create transition for {0} => {1}", node.title, targetNodeTitle);
                            }
                        }
                    }
                }

                Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteOnlyMissingNodes(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;
                IBaseState stateToClone = nodeBuffer.state;

                // DETECT MISSING STATES

                List<MissingStatesNodeData> missingStatesNodeDatas = new List<MissingStatesNodeData>();
                DetectMissingStates(nodeBuffer, node, ref missingStatesNodeDatas);

                var dict = new Dictionary<MissingStatesNodeData, Node>();
                foreach (var data in missingStatesNodeDatas)
                {
                    if (data.type.Equals(PasteSpacialType_AddMissingNode))
                    {
                        var com = data.targetParentNode.state as IComposedState;

                        Debug.LogFormat("Creating node {0} in {1}", data.sourceNode.title, Runtime.Utils.GetFullNodeName(data.targetParentNode, "/"));

                        var newNode = HFSMUtils.createNode(com);
                        newNode.title = data.sourceNode.title;
                        newNode.rect = data.sourceNode.rect;
                        newNode.transitions = new List<Transition>();

                        dict[data] = newNode;
                    }
                    else if (data.type.Equals(PasteSpacialType_AddMissingState))
                    {
                        var newNode = (data.targetParentNode.state as IComposedState).FindNodeByTitle(data.sourceNode.title);
                        if (newNode)
                        {
                            var newState = HFSMUtils.createState(data.sourceNode.state.GetType(), newNode);

                            var oldJson = EditorJsonUtility.ToJson(data.sourceNode.state);
                            EditorJsonUtility.FromJsonOverwrite(oldJson, newState);

                            Debug.LogFormat("Creating state {0} in {1}", data.sourceNode.GetType().Name, Runtime.Utils.GetFullNodeName(newNode, "/"));
                        }
                    }
                }

                foreach (var kvp in dict)
                {
                    var data = kvp.Key;
                    var newNode = kvp.Value;

                    foreach (var trans in data.sourceNode.transitions)
                    {
                        var targetNode = (newNode.parent.state as IComposedState).FindNodeByTitle(trans.node.title);
                        if (targetNode)
                        {
                            Debug.LogFormat("Creating transition {0} to {1}", Runtime.Utils.GetFullNodeName(newNode, "/"), Runtime.Utils.GetFullNodeName(targetNode, "/"));

                            var newTransition = HFSMUtils.createTransition(newNode, targetNode);
                            newTransition.weight = trans.weight;
                            newTransition.exitTime = trans.exitTime;
                            newTransition.hasExitTime = trans.hasExitTime;

                            foreach (var cond in trans.conditions)
                            {
                                Debug.LogFormat("Creating condition for {0} to {1}", Runtime.Utils.GetFullNodeName(newNode, "/"), Runtime.Utils.GetFullNodeName(targetNode, "/"));

                                var newCond = HFSMUtils.createCondition(newTransition);
                                newCond.bConstant = cond.bConstant;
                                newCond.fConstant = cond.fConstant;
                                newCond.iConstant = cond.iConstant;
                                newCond.nextOperand = cond.nextOperand;
                                newCond.operation = cond.operation;
                                newCond.sConstant = cond.sConstant;
                                newCond.value = newNode.layer.findValue(cond.value.name);
                            }
                        }
                    }

                    var sourceCom = data.sourceNode.parent.state as IComposedState;
                    foreach (var sourceComNode in sourceCom.nodes)
                    {
                        foreach (var trans in sourceComNode.transitions)
                        {
                            if (trans.node == data.sourceNode)
                            {
                                var targetNode = (newNode.parent.state as IComposedState).FindNodeByTitle(sourceComNode.title);
                                if (targetNode)
                                {
                                    Debug.LogFormat("Creating transition {0} to {1}", Runtime.Utils.GetFullNodeName(targetNode, "/"), Runtime.Utils.GetFullNodeName(newNode, "/"));

                                    var newTransition = HFSMUtils.createTransition(targetNode, newNode);
                                    newTransition.weight = trans.weight;
                                    newTransition.exitTime = trans.exitTime;
                                    newTransition.hasExitTime = trans.hasExitTime;

                                    foreach (var cond in trans.conditions)
                                    {
                                        Debug.LogFormat("Creating conditions for {0} to {1}", Runtime.Utils.GetFullNodeName(targetNode, "/"), Runtime.Utils.GetFullNodeName(newNode, "/"));

                                        var newCond = HFSMUtils.createCondition(newTransition);
                                        newCond.bConstant = cond.bConstant;
                                        newCond.fConstant = cond.fConstant;
                                        newCond.iConstant = cond.iConstant;
                                        newCond.nextOperand = cond.nextOperand;
                                        newCond.operation = cond.operation;
                                        newCond.sConstant = cond.sConstant;
                                        newCond.value = newNode.layer.findValue(cond.value.name);
                                    }
                                }
                            }
                        }
                    }
                }

                Repaint();
            }
        }

        private void DetectMissingStates(Node nodeSource, Node nodeDest, ref List<MissingStatesNodeData> data, bool detectVars = false)
        {
            if (nodeSource.state is IComposedState)
            {
                var comSource = nodeSource.state as IComposedState;

                var comDest = nodeDest.state as IComposedState;
                foreach (var childSourceNode in comSource.nodes)
                {
                    var childDestNode = comDest.FindNodeByTitle(childSourceNode.title);
                    if (childDestNode == null)
                    {
                        data.Add(new MissingStatesNodeData() { sourceNode = childSourceNode, targetParentNode = nodeDest, type = PasteSpacialType_AddMissingNode });
                    }
                    else if (childDestNode.state == null && childSourceNode.state != null)
                    {
                        data.Add(new MissingStatesNodeData() { sourceNode = childSourceNode, targetParentNode = nodeDest, type = PasteSpacialType_AddMissingState });
                    }
                    else if ((childDestNode.state != null && childSourceNode.state != null) && (childDestNode.state.GetType() != childSourceNode.state.GetType()))
                    {
                        data.Add(new MissingStatesNodeData() { sourceNode = childSourceNode, targetParentNode = nodeDest, type = PasteSpacialType_ChangeStateType });
                    }
                    else
                    {
                        DetectMissingStates(childSourceNode, childDestNode, ref data, true);
                    }

                    //var thisData = data.Find(x => x.sourceNode == childSourceNode && x.targetParentNode == nodeDest);
                    //if (thisData != null)
                    //{
                    //    foreach (var t in childSourceNode.transitions)
                    //    {
                    //        if ()
                    //    }
                    //}
                }
            }
            else if (detectVars && nodeSource != null && nodeDest != null && nodeSource.state != null && nodeDest.state != null && nodeSource.state.GetType() == nodeDest.state.GetType())
            {
                var objectSource = new SerializedObject(nodeSource.state);
                var objectDest = new SerializedObject(nodeDest.state);

                var diffs = new Dictionary<string, SerializedProperty[]>();
                DiffSerializedObjects(objectSource, objectDest, ref diffs);

                if (diffs.Count > 0)
                    data.Add(new MissingStatesNodeData() { sourceNode = nodeSource, targetParentNode = nodeDest.parent, changedProperties = diffs, type = PasteSpacialType_ChangeStateProperty });
            }

            //if (detectVars)
            //{
            //    if (nodeDest)
            //    {
            //        var thisMissingData = data.Find(x => x.sourceNode == nodeSource && x.targetParentNode == nodeDest.parent);
            //        if (thisMissingData != null)
            //        {
            //            var objectSource = new SerializedObject(nodeSource);
            //            var objectDest = new SerializedObject(nodeDest);

            //            DiffSerializedTransitions(objectSource, objectDest, ref thisMissingData.changedProperties);
            //        }
            //        else
            //        {
            //            var objectSource = new SerializedObject(nodeSource);
            //            var objectDest = new SerializedObject(nodeDest);

            //            var diffs = new Dictionary<string, SerializedProperty[]>();
            //            DiffSerializedTransitions(objectSource, objectDest, ref diffs);
            //            if (diffs.Count > 0)
            //                thisMissingData = new MissingStatesNodeData() { sourceNode = nodeSource, targetParentNode = nodeDest.parent, changedProperties = diffs, type = "Added Transitions" };
            //        }
            //    }
            //}
        }

        private static void DetectPropertyChanges(Node nodeSource, Node nodeDest, List<MissingStatesNodeData> data, SerializedObject objectSource, SerializedObject objectDest)
        {
            SerializedProperty propSource = objectSource.GetIterator();
            if (propSource.NextVisible(true))
            {
                do
                {
                    var miss = DetectPropertyChangesForProperties(nodeSource, nodeDest, data, objectDest, propSource, null);
                    if (miss != null)
                        data.Add(miss);
                }
                while (propSource.NextVisible(false));
            }
        }

        private static MissingStatesNodeData DetectPropertyChangesForProperties(Node nodeSource, Node nodeDest, List<MissingStatesNodeData> data, SerializedObject objectDest, SerializedProperty propSource, MissingStatesNodeData miss)
        {
            var propSourceName = propSource.propertyPath;
            var propDest = objectDest.FindProperty(propSourceName);
            if (!SerializedProperty.DataEquals(propSource, propDest))
            {
                if (propSource.isArray)
                {
                    for (var i = 0; i < propSource.arraySize; i++)
                    {
                        var miss2 = DetectPropertyChangesForProperties(nodeSource, nodeDest, data, objectDest, propSource.GetArrayElementAtIndex(i), miss);
                        if (miss2 != null && miss == null)
                        {
                            miss = miss2;
                        }
                    }
                }
                else if (!propSourceName.Equals("node"))
                {
                    if (miss == null)
                    {
                        miss = new MissingStatesNodeData() { sourceNode = nodeSource, targetParentNode = nodeDest.parent, changedProperties = new Dictionary<string, SerializedProperty[]>() };                        
                    }

                    miss.changedProperties.Add(propSource.propertyPath, new SerializedProperty[] { propSource.Copy(), propDest });
                }
            }

            return miss;
        }

        private static void DiffSerializedObjects(SerializedObject objectSource, SerializedObject objectDest, ref Dictionary<string, SerializedProperty[]> diffs)
        {
            SerializedProperty propSource = objectSource.GetIterator();
            if (propSource.Next(true))
            {
                do
                {
                    if (propSource.name.StartsWith("m_"))
                        continue;    

                    if (propSource.propertyPath.StartsWith("m_"))
                        continue;

                    if (propSource.propertyType == SerializedPropertyType.ArraySize)
                        continue;

                    if (propSource.propertyType == SerializedPropertyType.ObjectReference)
                        continue;

                    if (propSource.propertyType == SerializedPropertyType.Generic)
                        continue;

                    var propSourceName = propSource.propertyPath;
                    var idx = propSourceName.LastIndexOf(".");
                    if (idx > 0)
                    {
                        var propParentSourceName = propSourceName.Substring(0, idx);

                        var propParentSource = objectSource.FindProperty(propParentSourceName);
                        if (propParentSourceName.EndsWith("Array"))
                            continue;

                        if (propParentSource != null && propParentSource.propertyType == SerializedPropertyType.String)
                            continue;
                    }

                    var propDest = objectDest.FindProperty(propSourceName);
                    if (propDest != null)
                    {
                        if (!SerializedProperty.DataEquals(propSource, propDest))
                        {
                            diffs.Add(propSource.propertyPath, new SerializedProperty[] { propSource.Copy(), propDest });
                        }
                    }
                    else
                    {
                        diffs.Add(propSource.propertyPath, new SerializedProperty[] { propSource.Copy(), null });
                    }
                }
                while (propSource.Next(true));
            }
        }

        private void OnContextMenuItemClick_PasteState(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;

                IBaseState stateToClone = nodeBuffer.state;

                node.state = HFSMUtils.cloneTask(stateToClone, node, node.layer, CloneOptions.WithReplacementData(stateToClone, node.state, true, true));
                //stateToClone.node = node;
                HFSMUtils.removeState(stateToDelete);
                nodeBuffer = null;

                Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteNode(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                //Node nodeGroup = userData as Node;

                //IBaseState stateToDelete = nodeGroup.state;
                //IBaseState stateToClone = nodeBuffer.state;

                //nodeGroup.state = HFSMUtils.cloneTask(stateToClone, nodeGroup, nodeGroup.layer, HFSMUtils.CloneOptions.WithReplacementData(stateToClone, nodeGroup.state, true, true));
                //HFSMUtils.removeState(stateToDelete);
                //nodeBuffer = null;

                //Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteStateCustom(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;
                IBaseState stateToClone = nodeBuffer.state;

                PasteSpecialWindow.GetWindow(stateToDelete, stateToClone);
                nodeBuffer = null;

                Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteStateNoEvents(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;

                IBaseState stateToClone = nodeBuffer.state;

                node.state = HFSMUtils.cloneTask(stateToClone, node, node.layer, CloneOptions.WithReplacementData(stateToClone, node.state, false, true));
                //stateToClone.node = node;
                HFSMUtils.removeState(stateToDelete);
                nodeBuffer = null;

                Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteStateNoEventsOrAnimations(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;

                IBaseState stateToClone = nodeBuffer.state;

                node.state = HFSMUtils.cloneTask(stateToClone, node, node.layer, CloneOptions.WithReplacementData(stateToClone, node.state, false, false));
                //stateToClone.node = node;
                HFSMUtils.removeState(stateToDelete);
                nodeBuffer = null;

                Repaint();
            }
        }

        private void OnContextMenuItemClick_PasteStateNoAnimations(object userData)
        {
            if (nodeBuffer && nodeBuffer.state)
            {
                Node node = userData as Node;

                IBaseState stateToDelete = node.state;

                IBaseState stateToClone = nodeBuffer.state;

                node.state = HFSMUtils.cloneTask(stateToClone, node, node.layer, CloneOptions.WithReplacementData(stateToClone, node.state, true, false));
                //stateToClone.node = node;
                HFSMUtils.removeState(stateToDelete);
                nodeBuffer = null;

                Repaint();
            }
        }

        // SET AS ANY STATE
        private void OnContextMenuItemClick_HFSMSetAny(object userData)
        {
            Layer layer = this.layer as Layer;

            Node state = userData as Node;

            if (currentGroup)
            {
                currentGroup.anyNodeIndex = currentGroup.FindIdxByNode(state);
                EditorUtility.SetDirty(currentGroup);
            }

            Repaint();
        }

        // SET AS EXIT STATE
        private void OnContextMenuItemClick_HFSMSetExit(object userData)
        {
            Layer layer = this.layer as Layer;

            Node state = userData as Node;

            if (currentGroup)
            {
                currentGroup.exitNodeIndex = currentGroup.FindIdxByNode(state);
                EditorUtility.SetDirty(currentGroup);
            }

            Repaint();
        }

        #endregion

        #region MISC

        void setLayer(Layer newLayer)
        {
            layer = newLayer;

            if (layer && layer.root && layer.root.state is IComposedState)
            {
                currentGroup = layer.root.state as IComposedState;                
            }
            else
            {
                currentGroup = null;
            }

            OnSelectedGroupChanged();
        }

        void setGroup(IComposedState group)
        {
            currentGroup = group;

            OnSelectedGroupChanged();
        }

        void OnSelectedLayerChanged()
        {
        }

        void OnSelectedGroupChanged()
        {
        }

        #endregion
        
        #region Styles
        
        private GUIStyle _styleBox;
        private GUIStyle _styleBoxEmpty;
        private GUIStyle _styleParam;
        private GUIStyle _labelStateParamStyle;

        private GUIStyle LabelStateStyle
        {
            get
            {
                if (_styleBox != null) 
                    return _styleBox;
                
                _styleBox = new GUIStyle(GUI.skin.label);
                {
                    _styleBox.richText = true;
                    _styleBox.alignment = TextAnchor.UpperLeft;
                    _styleBox.fontStyle = FontStyle.Bold;
                    _styleBox.border = new RectOffset(7, 7, 40, 11);
                    _styleBox.imagePosition = ImagePosition.ImageLeft;
                }

                return _styleBox;
            }
        }
        
        private GUIStyle LabelEmptyStateStyle
        {
            get
            {
                if (_styleBoxEmpty != null)
                    return _styleBoxEmpty;
                
                _styleBoxEmpty = new GUIStyle(GUI.skin.label);
                {
                    _styleBoxEmpty.alignment = TextAnchor.MiddleCenter;
                    _styleBoxEmpty.fontStyle = FontStyle.Bold;
                    _styleBoxEmpty.border = new RectOffset(7, 11, 7, 11);
                }

                return _styleBoxEmpty;
            }
        }
        
        private GUIStyle LabelStateParamStyle
        {
            get
            {
                if (_labelStateParamStyle != null) 
                    return _labelStateParamStyle;
                
                _labelStateParamStyle = new GUIStyle(GUI.skin.label);
                {
                    _labelStateParamStyle.alignment = TextAnchor.MiddleRight;
                    _labelStateParamStyle.fontStyle = FontStyle.Bold;
                }

                return _labelStateParamStyle;
            }
        }
        
        #endregion
    }
}
