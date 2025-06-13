using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Behaviours.HFSM.Runtime;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace Behaviours.HFSM.Editor
{
    public class HFSMUtils
    {
        #region MENU

        [MenuItem("Behaviour/HFSM/Create HFSM Asset...")]
        static void BG_NEW_HFSM()
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Select a HFSM path...", "NewHFSM", "asset", "Creating a new HFSM...");
            if (string.IsNullOrEmpty(filePath) == false)
            {
                createLayer(filePath);

                AssetDatabase.ImportAsset(filePath);
            }
        }

        #endregion        

        public static Node FindNodeByTitle(Node parent, string title)
        {
            var ics = parent.state as IComposedState;
            return ics ? ics.nodes.Find(x => x.title.Equals(title)): null;
        }

        #region CREATE

        public static Layer createLayer(string assetPath, bool useCreateAsset = true)
        {
            Layer layer = null;

            if (useCreateAsset)
            {
                layer = CreateAsset<Layer>(assetPath);
            }
            else
            {
                layer = ScriptableObject.CreateInstance<Layer>();
                AssetDatabase.CreateAsset(layer, assetPath);
            }

            //
            {
                layer.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                layer.root = createNode();
                AssetDatabase.AddObjectToAsset(layer.root, layer);

                layer.root.layer = layer;
                layer.root.state = createState(typeof(IComposedState), layer.root);
            }
            EditorUtility.SetDirty(layer);            

            EditorUtility.SetDirty(layer.root);

            return layer;
        }

        public static Node createNode(IComposedState parent = null)
        {
            Node node = ScriptableObject.CreateInstance<Node>();
            {
                node.title = "Node";
            }

            node.rect = new Rect(0, 0, 200, 50);

            if (parent)
            {
                node.layer = parent.node.layer;
                node.parent = parent.node;

                parent.nodes.Add(node);

                AssetDatabase.AddObjectToAsset(node, node.layer);

                EditorUtility.SetDirty(parent);
            }

            EditorUtility.SetDirty(node);

            return node;
        }

        public static IBaseState createState(System.Type stateType, Node node, bool createDerivedNodes = true)
        {
            if (node.state)
            {
                removeState(node.state);
            }

            IBaseState state = ScriptableObject.CreateInstance(stateType) as IBaseState;
            {
                state.name = stateType.Name;
                state.node = node;
                state.hideFlags = HideFlags.HideInHierarchy;
            }

            node.state = state;

            if (createDerivedNodes)
            {
                if (state is IComposedState)
                {
                    IComposedState comp = state as IComposedState;

                    Node defaultNode = createNode(comp);
                    {
                        defaultNode.title = "Default";
                        defaultNode.rect.position = new Vector2(100 + (defaultNode.rect.width + 20) * 0, 200);
                        comp.defaultNodeIndex = 0;
                    }

                    Node anyNode = createNode(comp);
                    {
                        anyNode.title = "Any";
                        anyNode.rect.position = new Vector2(100 + (anyNode.rect.width + 20) * 1, 200);
                        comp.anyNodeIndex = 1;
                    }

                    if (node.parent)
                    {
                        Node exitNode = createNode(comp);
                        {
                            exitNode.title = "Exit";
                            exitNode.rect.position = new Vector2(100 + (exitNode.rect.width + 20) * 2, 200);
                        }
                        comp.exitNodeIndex = 2;
                    }
                }
            }

            AssetDatabase.AddObjectToAsset(state, node.layer);

            EditorUtility.SetDirty(node);

            return state;
        }

        public static Transition createTransition(Node node, Node targetNode)
        {
            Transition transition = ScriptableObject.CreateInstance<Transition>();
            {
                transition.name = "Transition";
                transition.node = targetNode;
                transition.weight = 1;
                transition.hideFlags = HideFlags.HideInHierarchy;
            }

            node.transitions.Add(transition);

            AssetDatabase.AddObjectToAsset(transition, node.layer);
            EditorUtility.SetDirty(transition);
            EditorUtility.SetDirty(node);

            return transition;
        }

        public static Transition GetOrAddTransitionWhere(Node node, Node targetNode, System.Func<Transition, bool> testFunc, System.Action<Transition> onAdd = null)
        {
            Transition transition = node.transitions.Find(x => (x.node == targetNode) && testFunc(x));
            if (!transition)
            {
                transition = ScriptableObject.CreateInstance<Transition>();

                transition.name = "Transition";
                transition.node = targetNode;
                transition.weight = 1;
                transition.hideFlags = HideFlags.HideInHierarchy;

                node.transitions.Add(transition);

                onAdd?.Invoke(transition);

                AssetDatabase.AddObjectToAsset(transition, node.layer);
                EditorUtility.SetDirty(transition);
                EditorUtility.SetDirty(node);
            }

            return transition;
        }

        public static Transition GetOrAddTransition(Node node, Node targetNode)
        {
            Transition transition = node.transitions.Find(x => x.node == targetNode);
            if (!transition)
            {
                Debug.LogFormat("Creating transition from {0} to {1}...", node.title, targetNode.title);

                transition = ScriptableObject.CreateInstance<Transition>();

                transition.name = "Transition";
                transition.node = targetNode;
                transition.weight = 1;
                transition.hideFlags = HideFlags.HideInHierarchy;

                node.transitions.Add(transition);

                AssetDatabase.AddObjectToAsset(transition, node.layer);
                EditorUtility.SetDirty(transition);
                EditorUtility.SetDirty(node);
            }

            return transition;
        }

        public static Transition GetOrAddTransition(Node node, Node targetNode, Value value)
        {
            var transitions = node.transitions.FindAll(x => x.node == targetNode);
            foreach (var transition in transitions)
            {
                foreach (var condition in transition.conditions)
                {
                    if (condition.value == value)
                        return transition;
                }
            }

            // CREATE A NEW TRANSITION
            {
                Transition transition = ScriptableObject.CreateInstance<Transition>();

                transition.name = "Transition";
                transition.node = targetNode;
                transition.weight = 1;
                transition.hideFlags = HideFlags.HideInHierarchy;

                node.transitions.Add(transition);

                AssetDatabase.AddObjectToAsset(transition, node.layer);
                EditorUtility.SetDirty(transition);
                EditorUtility.SetDirty(node);

                return transition;
            }
        }

        public static Transition AddTransition(Node node, Node targetNode)
        {
            Transition transition = ScriptableObject.CreateInstance<Transition>();

            transition.name = "Transition";
            transition.node = targetNode;
            transition.weight = 1;
            transition.hideFlags = HideFlags.HideInHierarchy;

            node.transitions.Add(transition);

            AssetDatabase.AddObjectToAsset(transition, node.layer);
            EditorUtility.SetDirty(transition);
            EditorUtility.SetDirty(node);

            return transition;
        }

        public static void removeAllServices(Node node)
        {
            var services = new List<IService>(node.services);
            foreach (var service in services)
            {
                DeleteService(node, service);
            }
        }

        public static void removeAllTransitions(Node node)
        {
            var transitions = new List<Transition>(node.transitions);
            foreach (var transition in transitions)
            {
                removeTransition(transition, node);
            }
        }

        public static void removeAllTransitions(Node node, Node nodeTo)
        {
            var transitions = new List<Transition>(node.transitions);
            foreach (var transition in transitions)
            {
                if (transition.node == nodeTo)
                {
                    removeTransition(transition, node);
                }
            }
        }

        public static T GetOrAddState<T>(Node node) where T : IBaseState
        {
            if (node.state != null)
            {
                if ((node.state is T) == false)
                {
                    createState(typeof(T), node, false);
                }
            }
            else
            {
                createState(typeof(T), node, false);
            }

            return node.state as T; 
        }

        public static Node GetOrCloneNode(IComposedState group, string title, Node nodeToClone)
        {
            var n = group.nodes.Find(x => x.title.Equals(title));
            if (n == null)
            {
                n = cloneNode(nodeToClone, group.node, group.node.layer, CloneOptions.Default);
                n.title = title;
        
                group.nodes.Add(n);
            }
            return n;
        }

        public static Node GetOrAddNode(IComposedState group, string title, System.Action<Node> onCreate = null)
        {
            var n = group.nodes.Find(x => x.title.Equals(title));
            if (n == null)
            {
                Debug.LogFormat("Creating node {0}...", title);
                n = createNode(group);
                n.title = title;
                onCreate?.Invoke(n);
            }

            return n;
        }

        public static Condition GetOrAddConditionBool(Transition transition, Value value, Operation op, bool c, Operand next)
        {
            var condition = transition.conditions.Find(x => x.value == value);
            if (condition == null)
            {
                condition = createCondition(transition);
                condition.value = value;
            }

            condition.operation = op;
            condition.bConstant = c;
            condition.nextOperand = next;
            return condition;
        }

        public static Condition GetOrAddConditionFloat(Transition transition, Value value, Operation op, float c, Operand next)
        {
            var condition = transition.conditions.Find(x => x.value == value);
            if (condition == null)
            {
                condition = createCondition(transition);
                condition.value = value;
            }

            condition.operation = op;
            condition.fConstant = c;
            condition.nextOperand = next;
            return condition;
        }

        public static Condition GetOrAddConditionFloatWhere(Transition transition, Value value, System.Func<Condition, bool> func, Operation op, float c, Operand next)
        {
            var condition = transition.conditions.Find(x => x.value == value && func(x) );
            if (condition == null)
            {
                condition = createCondition(transition);
                condition.value = value;
            }

            condition.operation = op;
            condition.fConstant = c;
            condition.nextOperand = next;
            return condition;
        }

        public static Condition GetOrAddConditionInt(Transition transition, Value value, Operation op, int c, Operand next, bool checkValue = false)
        {
            var condition = transition.conditions.Find(x => checkValue ? (x.value == value && x.iConstant == c) : (x.value == value));
            if (condition == null)
            {
                condition = createCondition(transition);
                condition.value = value;
            }

            condition.operation = op;
            condition.iConstant = c;
            condition.nextOperand = next;
            return condition;
        }

        public static Condition createCondition(Transition transition)
        {
            Condition condition = ValueUtils.createCondition();

            if (transition)
            {
                transition.conditions.Add(condition);

                EditorUtility.SetDirty(condition);
                AssetDatabase.AddObjectToAsset(condition, transition.node.layer);
                EditorUtility.SetDirty(transition);
            }

            return condition;
        }        

        #endregion        

        #region REMOVE        

        public static void RemoveNode(Node node, IComposedState parent = null)
        {
            if (node)
            {
                if (parent)
                {
                    parent.nodes.Remove(node);
                    EditorUtility.SetDirty(parent);

                    for (int i = 0; i < parent.nodes.Count; ++i)
                    {
                        for (int j = 0; j < parent.nodes[i].transitions.Count; ++j)
                        {
                            if ((parent.nodes[i].transitions[j] != null) && (parent.nodes[i].transitions[j].node == node))
                            {
                                removeTransition(parent.nodes[i].transitions[j]);
                                EditorUtility.SetDirty(parent.nodes[i]);
                            }
                        }
                    }
                }

                removeAllTransitions(node);

                removeAllServices(node);

                removeState(node.state);

                Object.DestroyImmediate(node, true);
            }
        }

        public static void removeState(IBaseState state, Node parent = null)
        {
            if (state)
            {
                if (state is IComposedState)
                {
                    IComposedState comp = state as IComposedState;
                    for (int i = 0; i < comp.nodes.Count; ++i)
                    {
                        RemoveNode(comp.nodes[i]);
                    }
                    Object.DestroyImmediate(comp, true);
                }
                else
                {
                    Object.DestroyImmediate(state, true);
                }
            }
        }

        public static void removeTransition(Transition transition, Node parent = null)
        {
            if (transition)
            {
                if (parent)
                {
                    Debug.LogFormat("Removing transition from {0} to {1}...", parent.title, transition.node ? transition.node.title : "?");

                    parent.transitions.Remove(transition);
                    EditorUtility.SetDirty(parent);
                }
                else
                {
                    Debug.LogFormat("Removing transition to {0}...", transition.node ? transition.node.title : "?");
                }

                for (int i = 0; i < transition.conditions.Count; ++i)
                {
                    removeCondition(transition.conditions[i]);
                }

                Object.DestroyImmediate(transition, true);
            }
        }

        public static void removeCondition(Condition condition, Transition parent = null)
        {
            if (condition)
            {
                if (parent)
                {                    
                    parent.conditions.Remove(condition);
                    EditorUtility.SetDirty(parent);
                }

                Debug.LogFormat("Removing condition {0}", condition.value ? condition.value.name : "?");
                ValueUtils.removeCondition(condition);
            }
        }

        #endregion
        
        public static Node cloneNode(Node originalNode, Node parent, Layer layer, CloneOptions options)
        {
            if (originalNode != null)
            {
                Node newNode = ScriptableObject.Instantiate(originalNode);
                {
                    AssetDatabase.AddObjectToAsset(newNode, layer);
                }
        
                newNode.parent = parent;
                newNode.layer = layer;
        
                newNode.state = cloneTask(originalNode.state, newNode, layer, options);
        
                EditorUtility.SetDirty(newNode);
        
                if (parent)
                {
                    EditorUtility.SetDirty(parent);
                }
        
                return newNode;
            }
        
            return null;
        }
        
        public static IBaseState cloneTask(IBaseState originalTask, Node containerNode, Layer layer, CloneOptions options)
        {
            if (!originalTask) 
                return null;
            
            var newTask = ScriptableObject.Instantiate(originalTask);
            AssetDatabase.AddObjectToAsset(newTask, layer);
            newTask.node = containerNode;
        
            if (newTask is IComposedState composedState)
            {
                var originalComp = originalTask as IComposedState;
                for (var i = 0; i < composedState.nodes.Count; ++i)
                {
                    composedState.nodes[i] = cloneNode(composedState.nodes[i], containerNode, layer, options);
                }
        
                for (int i = 0; i < originalComp.nodes.Count; ++i)
                {
                    for (int j = 0; j < originalComp.nodes[i].transitions.Count; ++j)
                    {
                        var originalTransition = originalComp.nodes[i].transitions[j];
        
                        var nodeIndexInOriginalComp = originalComp.nodes.IndexOf(originalTransition.node);
                        var newNodeByIndexInOriginalComp = composedState.nodes[nodeIndexInOriginalComp];
        
                        var newTransition = cloneTransition(originalTransition, newNodeByIndexInOriginalComp, layer, options);
                        composedState.nodes[i].transitions[j] = newTransition;
                    }
                }
            }
            
            // else if (newTask is ConditionalSpineState conditionalSpineState)
            // {
            //     var path = CloneOptions.GetPath(newTask);
            //
            //     for (var i = 0; i < conditionalSpineState.options.Count; i++)
            //     {
            //         var option = conditionalSpineState.options[i];
            //         var token = string.Join("", option.conditions.Select(x => x.ToToken));
            //         //option.conditions.ForEach(x => token += x.ToToken);
            //         
            //         var newPath = path + $"{token}";
            //
            //         if (!options.cloneAnimations)
            //         {
            //             if (options.replaceData.TryGetValue(newPath, out var nodeData))
            //             {
            //                 Debug.LogFormat("{0} => {1} ~ {2}", conditionalSpineState.options[i].animation.animationName, nodeData.animationName, token);
            //
            //                 conditionalSpineState.options[i].animation.animationName = nodeData.animationName;
            //                 conditionalSpineState.options[i].animation.animationStart = nodeData.animationStart;
            //                 conditionalSpineState.options[i].animation.animationEnd = nodeData.animationEnd;
            //                 conditionalSpineState.options[i].animation.animationSpeed = nodeData.animationSpeed;
            //                 conditionalSpineState.options[i].animation.animationTrack = nodeData.animationTrack;
            //             }
            //             else
            //             {
            //                 Debug.LogFormat("{0} => {1} ~ {2}", conditionalSpineState.options[i].animation.animationName, "NULL", token);
            //             }
            //         }
            //
            //         if (!options.cloneEvents)
            //         {
            //             NodeData nodeData;
            //             if (options.replaceData.TryGetValue(newPath, out nodeData))
            //             {
            //                 conditionalSpineState.options[i].animation.events = nodeData.animationEvents;
            //             }
            //         }
            //     }
            // }
            
            else if (newTask is INodeDataProvider nodeDataProvider)
            {
                nodeDataProvider.SetNodeData(options);
            }
            
            // else if (newTask is SpineState spineState)
            // {
            //     if (!options.cloneAnimations)
            //     {
            //         var path = CloneOptions.GetPath(newTask);
            //         if (options.replaceData.TryGetValue(path, out var nodeData))
            //         {
            //             spineState.animationName = nodeData.animationName;
            //             spineState.animationSpeed = nodeData.animationSpeed;
            //             spineState.animationStart = nodeData.animationStart;
            //             spineState.animationEnd = nodeData.animationEnd;
            //             spineState.duration = nodeData.animationDuration;
            //             spineState.animationTrack = nodeData.animationTrack;                            
            //             spineState.events = nodeData.animationEvents.ToList();                            
            //         }
            //         else
            //         {
            //             Debug.LogFormat("{0} => {1}", spineState.animationName, "NULL");
            //         }
            //     }
            //
            //     if (!options.cloneEvents)
            //     {
            //         var path = CloneOptions.GetPath(newTask);
            //         NodeData nodeData;
            //         if (options.replaceData.TryGetValue(path, out nodeData))
            //         {
            //             spineState.events = nodeData.animationEvents;
            //         }
            //     }
            // }
        
            EditorUtility.SetDirty(newTask);
        
            if (containerNode)
            {
                EditorUtility.SetDirty(containerNode);
            }
        
            return newTask;
        }

        public static IService cloneService(IService orginalService, Node containerNode, Layer layer)
        {
            if (orginalService != null)
            {
                var newService = ScriptableObject.Instantiate(orginalService);
                {
                    AssetDatabase.AddObjectToAsset(newService, layer);
                }

                newService.node = containerNode;

                EditorUtility.SetDirty(newService);

                if (containerNode)
                {
                    EditorUtility.SetDirty(containerNode);
                }

                return newService;
            }
            return null;
        }

        public static void MoveTransitionTo(Node node, Transition transition, int nextIdx)
        {
            int thisIdx = node.transitions.IndexOf(transition);

            if (thisIdx == -1)
            {
                node.transitions.Insert(nextIdx, transition);

                EditorUtility.SetDirty(transition.node);
            }
            else if (thisIdx != nextIdx)
            {
                node.transitions.RemoveAt(thisIdx);
                node.transitions.Insert(nextIdx, transition);

                EditorUtility.SetDirty(transition.node);
            }
        }

        public static Transition cloneTransition(Transition originalTransition, Node parent, Layer asset, CloneOptions options)
        {
            if (originalTransition != null)
            {
                Transition newTransition = ScriptableObject.Instantiate(originalTransition);
                {
                    AssetDatabase.AddObjectToAsset(newTransition, asset);
                }
        
                newTransition.node = parent;
        
                // ADD CONDITIONS
                for (int i = 0; i < newTransition.conditions.Count; i++)
                {
                    newTransition.conditions[i] = cloneCondition(newTransition.conditions[i], newTransition, asset, options);
                }
        
                EditorUtility.SetDirty(newTransition);
        
                if (parent)
                {
                    EditorUtility.SetDirty(parent);
                }
        
                return newTransition;
            }
        
            return null;
        }
        
        public static Condition cloneCondition(Condition originalCondition, Transition parent, Layer layer, CloneOptions options)
        {
            if (originalCondition != null)
            {
                Condition newCondition = ScriptableObject.Instantiate(originalCondition);
                {
                    if (originalCondition.value)
                    {
                        newCondition.value = layer.findValue(originalCondition.value.name);
                    }
        
                    AssetDatabase.AddObjectToAsset(newCondition, layer);
                }
        
                EditorUtility.SetDirty(newCondition);
        
                return newCondition;
            }
        
            return null;
        }

        

        #region MISC

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        static T1 CreateAsset<T1>(string assetPath) where T1 : ScriptableObject
        {
            T1 asset = ScriptableObject.CreateInstance<T1>();

            string assetFileName = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.GetDirectoryName(assetPath) + "/" + System.IO.Path.GetFileNameWithoutExtension(assetPath) + ".asset");

            AssetDatabase.CreateAsset(asset, assetFileName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            return asset;
        }

        public static void CollectStatesRecursive(IComposedState group, List<IBaseState> touched)
        {
            foreach (var node in group.nodes)
            {
                var state = node.state;
                if (!state)
                    continue;
                
                if (state is IComposedState composedState)
                {
                    CollectStatesRecursive(composedState, touched);
                }
                else
                {
                    touched.Add(state);
                }

                if (state.node) 
                    continue;
                
                state.node = node;
                EditorUtility.SetDirty(state);
                Debug.LogWarningFormat("Fixed State {0} has a NULL parent node!", state.GetType().Name);
            }
        }

        #endregion

        #region IMPORT

        public static void importNodeFromJSON(Node node, JObject jc)
        {
            node.title = jc["name"].ToString();
            node.rect = new Rect(jc["rect"]["x"].ToObject<float>(), jc["rect"]["y"].ToObject<float>(), jc["rect"]["w"].ToObject<float>(), jc["rect"]["h"].ToObject<float>());

            if (jc.ContainsKey("state"))
            {
                System.Reflection.Assembly currentAssembly = System.Reflection.Assembly.GetAssembly(typeof(IBaseState));
                System.Type nodeType = currentAssembly.GetType(jc["state"]["type"].ToString());

                node.state = createState(nodeType, node, false);

                if (node.state is IComposedState)
                {                    
                    importGroupFromJSON(node, node.state as IComposedState, jc["state"] as JObject);
                }
                else
                {
                    node.state.deserialize(jc["state"] as JObject);
                }
            }
        }

        public static void importGroupFromJSON(Node parent, IComposedState comp, JObject jc)
        {
            comp.anyNodeIndex = jc["anyNodeIndex"].ToObject<int>();
            comp.defaultNodeIndex = jc["defaultNodeIndex"].ToObject<int>();
            comp.exitNodeIndex = jc["exitNodeIndex"].ToObject<int>();

            JArray ja = jc["nodes"] as JArray;
            for (int i = 0; i < ja.Count; ++i)
            {
                Node node = createNode(comp);
                {
                    importNodeFromJSON(node, ja[i] as JObject);
                }
            }

            ja = jc["transitions"] as JArray;
            for (int i = 0; i < ja.Count; ++i)
            {
                Node nodeFrom = comp.nodes[ja[i]["nodeFrom"].ToObject<int>()];
                Node nodeTo = comp.nodes[ja[i]["nodeTo"].ToObject<int>()];

                importTransition(nodeFrom, nodeTo, ja[i] as JObject);
            }
        }

        public static void importTransition(Node nodeFrom, Node nodeTo, JObject jc)
        {
            Transition transition = createTransition(nodeFrom, nodeTo);
            {
                transition.weight = jc["weight"].ToObject<float>();
                transition.hasExitTime = jc["hasExitTime"].ToObject<bool>();
                transition.exitTime = jc["exitTime"].ToObject<float>();
                JArray ja = jc["conditions"] as JArray;

                for (int j = 0; j < ja.Count; ++j)
                {
                    importCondition(transition, nodeFrom.layer, ja[j] as JObject);
                }
            }
        }

        public static void importCondition(Transition transition, Layer layer, JObject jc)
        {
            Condition condition = createCondition(transition);
            {
                condition.value = layer.findValue(jc["name"].ToString());

                condition.operation = (Operation)jc["operation"].ToObject<int>();
                condition.nextOperand = (Operand)jc["nextOperand"].ToObject<int>();

                ValueType type = (ValueType)jc["type"].ToObject<int>();

                if (type == ValueType.Integer)
                {
                    condition.iConstant = jc["constant"].ToObject<int>();
                }
                else if (type == ValueType.Float)
                {
                    condition.fConstant = jc["constant"].ToObject<float>();
                }
                else if (type == ValueType.Bool)
                {
                    condition.bConstant = jc["constant"].ToObject<bool>();
                }
                else
                {
                    condition.sConstant = jc["constant"].ToString();
                }
            }
        }

        #endregion

        #region EXPORT

        public static JObject exportNodeToJSON(Node node)
        {
            JObject jc = new JObject();
            {
                jc["name"] = new JValue(node.title);                

                jc["rect"] = new JObject();
                {
                    jc["rect"]["x"] = new JValue(node.rect.x);
                    jc["rect"]["y"] = new JValue(node.rect.y);
                    jc["rect"]["w"] = new JValue(node.rect.width);
                    jc["rect"]["h"] = new JValue(node.rect.height);
                }                

                if (node.state != null)
                {
                    if (node.state is IComposedState)
                    {
                        jc["state"] = exportGroupToJSON(node.state as IComposedState);
                    }
                    else
                    {
                        jc["state"] = exportStateToJSON(node.state);
                    }
                }
            }
            return jc;
        }

        public static JArray exportTransitionsToJSON(IComposedState comp)
        {
            JArray ja = new JArray();
            {
                for (int i = 0; i < comp.nodes.Count; ++i)
                {
                    for (int j = 0; j < comp.nodes[i].transitions.Count; ++j)
                    {
                        JObject jc = new JObject();
                        {                            
                            Node nodeFrom = comp.nodes[i];
                            Node nodeTo = nodeFrom.transitions[j].node;

                            jc["nodeFrom"] = new JValue(comp.nodes.IndexOf(nodeFrom));
                            jc["nodeTo"] = new JValue(comp.nodes.IndexOf(nodeTo));
                            jc["weight"] = new JValue(nodeFrom.transitions[j].weight);
                            jc["exitTime"] = new JValue(nodeFrom.transitions[j].exitTime);
                            jc["hasExitTime"] = new JValue(nodeFrom.transitions[j].hasExitTime);

                            jc["conditions"] = exportConditionsToJSON(nodeFrom.transitions[j]);
                        }
                        ja.Add(jc);
                    }
                }
            }
            return ja;
        }

        public static JArray exportConditionsToJSON(Transition transition)
        {
            JArray ja = new JArray();
            {
                for (int i = 0; i < transition.conditions.Count; ++i)
                {
                    if (transition.conditions[i].value)
                    {
                        JObject jc = new JObject();
                        {
                            jc["name"] = new JValue(transition.conditions[i].value.name);
                            jc["type"] = new JValue((int)transition.conditions[i].value.type);
                            jc["operation"] = new JValue((int)transition.conditions[i].operation);
                            jc["nextOperand"] = new JValue((int)transition.conditions[i].nextOperand);

                            if (transition.conditions[i].value.type == ValueType.Integer)
                            {
                                jc["constant"] = new JValue(transition.conditions[i].iConstant);
                            }
                            else if (transition.conditions[i].value.type == ValueType.Float)
                            {
                                jc["constant"] = new JValue(transition.conditions[i].fConstant);
                            }
                            else if (transition.conditions[i].value.type == ValueType.Bool)
                            {
                                jc["constant"] = new JValue(transition.conditions[i].bConstant);
                            }
                            else
                            {
                                jc["constant"] = new JValue(transition.conditions[i].sConstant);
                            }                            
                        }
                        ja.Add(jc);
                    }
                }
            }
            return ja;
        }

        public static JObject exportStateToJSON(IBaseState state)
        {
            return state.serialize();
        }

        public static JObject exportGroupToJSON(IComposedState group)
        {
            JObject jc = new JObject();
            {
                jc["type"] = group.GetType().FullName;

                jc["anyNodeIndex"] = new JValue(group.anyNodeIndex);
                jc["defaultNodeIndex"] = new JValue(group.defaultNodeIndex);
                jc["exitNodeIndex"] = new JValue(group.exitNodeIndex);

                JArray ja = new JArray();
                foreach (Node node in group.nodes)
                {
                    ja.Add(exportNodeToJSON(node));
                }
                jc["nodes"] = ja;

                jc["transitions"] = exportTransitionsToJSON(group);
            }
            return jc;
        }

        #endregion

        public static void OptimizeGroupNodes(IComposedState group)
        {
            if (group)
            {
                if (group.defaultNodeIndex != 0 || group.anyNodeIndex != 1 || group.exitNodeIndex != 2)
                {
                    var nodeDefault = group.GetDefaultNode();
                    var nodeAny = group.GetAnyNode();
                    var nodeExit = group.GetExitNode();

                    group.nodes.Remove(nodeDefault);
                    group.nodes.Remove(nodeAny);
                    group.nodes.Remove(nodeExit);

                    group.nodes.Insert(0, nodeExit);
                    group.nodes.Insert(0, nodeAny);
                    group.nodes.Insert(0, nodeDefault);

                    group.defaultNodeIndex = 0;
                    group.anyNodeIndex = 1;
                    group.exitNodeIndex = 2;

                    EditorUtility.SetDirty(group);
                }
            }
        }

        public static void AddService(Node targetNode, System.Type type)
        {
            var service = (IService)ScriptableObject.CreateInstance(type);

            service.name = "Service";
            service.node = targetNode;
            service.hideFlags = HideFlags.HideInHierarchy;

            targetNode.services.Add(service);

            AssetDatabase.AddObjectToAsset(service, targetNode.layer);
            EditorUtility.SetDirty(service);
            EditorUtility.SetDirty(targetNode);
        }

        public static void DeleteService(Node targetNode, IService service)
        {
            targetNode.services.Remove(service);
            EditorUtility.SetDirty(targetNode);

            Object.DestroyImmediate(service, true);
        }
    }
}