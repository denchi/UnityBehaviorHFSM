using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Behaviours.Runtime;

namespace Behaviours.HFSM.Runtime
{
    public static class Utils
    {
        public static RuntimeLayerData CreateWithLayer(Layer layer, GameObject gameObject, IList services = null)
        {
            var data = new RuntimeLayerData();
            {
                data.gameObject = gameObject;
                data.associatedLayer = layer;
                data.runtimeValues = new RuntimeValueCollection();
                {
                    data.runtimeValues.InitWithLayer(layer);
                }

                if (services != null)
                {
                    foreach (var service in services)
                    {
                        data.AddService(service);
                    }
                }

                data.runtimeRootGroupData = CreateWithGroup(layer.composedState, layer.root, null, data);

                CreateTransitionsForGroup(data.runtimeRootGroupData);
            }
            return data;
        }

        public static RuntimeConditionData CreateRuntimeCondition(RuntimeValueCollection runtimeValues, Condition condition)
        {
            var runtimeCondition = new RuntimeConditionData();
            {
                runtimeCondition.nextOperand = condition.nextOperand;
                runtimeCondition.operation = condition.operation;
                runtimeCondition.value = runtimeValues.GetRuntimeValue(condition.value.name);

                switch (condition.value.type)
                {
                    case ValueType.Bool: runtimeCondition.bConstant = condition.bConstant; break;
                    case ValueType.Integer: runtimeCondition.iConstant = condition.iConstant; break;
                    case ValueType.Float: runtimeCondition.fConstant = condition.fConstant; break;
                    case ValueType.String: runtimeCondition.sConstant = condition.sConstant; break;
                    case ValueType.Other: runtimeCondition.sConstant = condition.sConstant; break;
                }
            }

            return runtimeCondition;
        }

        public static string GetFullNodeName(Node node, string separator)
        {
            if (!node)
                return "NULL";

            var nodeName = node.title;
            while (node.parent)
            {
                nodeName = node.parent.title + separator + nodeName;
                node = node.parent;
            }

            return nodeName;
        }
        
        private static RuntimeGroupData CreateWithGroup(IComposedState comp, Node node, RuntimeGroupData parent, RuntimeLayerData layerData)
        {
            var runtimeGroupData = new RuntimeGroupData();
            {
                runtimeGroupData.runtimeLayerData = layerData;

                runtimeGroupData.parentGroup = parent;
                runtimeGroupData.associatedNode = node;
                runtimeGroupData.runtimeStateData = new RuntimeStateData();
                {
                    runtimeGroupData.runtimeStateData.runtimeGroupData = runtimeGroupData;
                    runtimeGroupData.runtimeStateData.runtimeNodeData = runtimeGroupData;
                    runtimeGroupData.runtimeStateData.associatedState = comp;
                }

                for (int i = 0; i < comp.nodes.Count; ++i)
                {
                    if (comp.nodes[i])
                    {
                        RuntimeNodeData childRuntimeNodeData = null;

                        if (comp.nodes[i].state is IComposedState)
                        {
                            childRuntimeNodeData = CreateWithGroup(comp.nodes[i].state as IComposedState, comp.nodes[i], runtimeGroupData, layerData);
                        }
                        else
                        {
                            childRuntimeNodeData = CreateWithState(comp.nodes[i].state, comp.nodes[i], runtimeGroupData, layerData);
                        }

                        if (i == comp.defaultNodeIndex)
                        {
                            runtimeGroupData.defaultNode = childRuntimeNodeData;
                        }

                        if (i == comp.anyNodeIndex)
                        {
                            runtimeGroupData.anyNode = childRuntimeNodeData;
                        }

                        if (i == comp.exitNodeIndex)
                        {
                            runtimeGroupData.exitNode = childRuntimeNodeData;
                        }

                        runtimeGroupData.nodes.Add(childRuntimeNodeData);
                    }
                }

                if (runtimeGroupData.associatedNode.services != null)
                {
                    runtimeGroupData.runtimeServices = new List<RuntimeServiceData>();
                    for (int i = 0, n = runtimeGroupData.associatedNode.services.Count; i < n; i++)
                    {
                        var service = runtimeGroupData.associatedNode.services[i];
                        var runtimeService = service.createRuntimeServiceData(runtimeGroupData);
                        runtimeGroupData.runtimeServices.Add(runtimeService);
                    }
                }
            }
            return runtimeGroupData;
        }

        private static RuntimeNodeData CreateWithState(IBaseState state, Node node, RuntimeGroupData parent, RuntimeLayerData layerData)
        {
            var runtimeNodeData = new RuntimeNodeData();
            {
                runtimeNodeData.runtimeLayerData = layerData;

                runtimeNodeData.parentGroup = parent;
                runtimeNodeData.associatedNode = node;

                if (state)
                {
                    runtimeNodeData.runtimeStateData = state.createRuntimeData(parent.runtimeLayerData);
                }
                else
                {
                    runtimeNodeData.runtimeStateData = new RuntimeStateData();
                }

                runtimeNodeData.runtimeStateData.runtimeNodeData = runtimeNodeData;
                runtimeNodeData.runtimeStateData.associatedState = state;

                if (runtimeNodeData.associatedNode.services != null)
                {
                    runtimeNodeData.runtimeServices = new List<RuntimeServiceData>();
                    for (int i = 0, n = runtimeNodeData.associatedNode.services.Count; i < n; i++)
                    {
                        var service = runtimeNodeData.associatedNode.services[i];
                        var runtimeService = service.createRuntimeServiceData(runtimeNodeData);
                        runtimeNodeData.runtimeServices.Add(runtimeService);
                    }
                }
            }
            return runtimeNodeData;
        }

        private static void CreateTransitionsForGroup(RuntimeGroupData group)
        {
            for (var i = 0; i < group.nodes.Count; ++i)
            {
                var node = group.nodes[i].associatedNode;

                var runtimeNode = group.nodes[i];
                {
                    runtimeNode.transitions = new List<RuntimeTransitionData>();

                    for (int j = 0; j < group.nodes[i].associatedNode.transitions.Count; ++j)
                    {
                        Transition transition = group.nodes[i].associatedNode.transitions[j];

                        Debug.AssertFormat(transition, "NULL transition N#{0} from {1}", j, group.nodes[i].associatedNode.getSimplifiedPath());

                        RuntimeTransitionData runtimeTransition = new RuntimeTransitionData();
                        {
                            runtimeTransition.weight = transition.weight;
                            runtimeTransition.exitTime = transition.exitTime;
                            runtimeTransition.hasExitTime = transition.hasExitTime;

                            runtimeTransition.node = RuntimeNodeFromNode(group, transition.node);

                            CreateConditionsForTransition(runtimeTransition, transition, group.runtimeLayerData);
                        }
                        runtimeNode.transitions.Add(runtimeTransition);
                    }

                    if (runtimeNode is RuntimeGroupData)
                    {
                        CreateTransitionsForGroup(runtimeNode as RuntimeGroupData);
                    }
                }
            }
        }

        private static void CreateConditionsForTransition(RuntimeTransitionData runtimeTransition, Transition transition, RuntimeLayerData runtimeLayer)
        {
            runtimeTransition.conditions = new List<RuntimeConditionData>();

            for (int i = 0; i < transition.conditions.Count; ++i)
            {
                Condition condition = transition.conditions[i];

                RuntimeConditionData runtimeCondition = new RuntimeConditionData();
                {
                    runtimeCondition.nextOperand = condition.nextOperand;
                    runtimeCondition.operation = condition.operation;

#if UNITY_EDITOR
                    UnityEngine.Assertions.Assert.IsNotNull(condition.value, "Condition to node " + GetFullNodeName(transition.node, ":") + " is NULL!!!");
#endif

                    runtimeCondition.value = runtimeLayer.runtimeValues.GetRuntimeValue(condition.value.name);

                    switch (condition.value.type)
                    {
                        case ValueType.Bool: runtimeCondition.bConstant = condition.bConstant; break;
                        case ValueType.Integer: runtimeCondition.iConstant = condition.iConstant; break;
                        case ValueType.Float: runtimeCondition.fConstant = condition.fConstant; break;
                        case ValueType.String: runtimeCondition.sConstant = condition.sConstant; break;
                        case ValueType.Other: runtimeCondition.sConstant = condition.sConstant; break;
                    }
                }

                runtimeTransition.conditions.Add(runtimeCondition);
            }
        }

        private static RuntimeNodeData RuntimeNodeFromNode(RuntimeGroupData group, Node node)
        {
            return group.nodes.Find(x => x.associatedNode == node);
        }
    }
}