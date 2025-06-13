using System.Collections.Generic;

using Behaviours.Runtime;

namespace Behaviours.HFSM.Runtime
{
    public class RuntimeNodeData
    {
        public RuntimeGroupData parentGroup;
        public RuntimeStateData runtimeStateData;
        public Node associatedNode;
        public RuntimeLayerData runtimeLayerData;

        public List<RuntimeTransitionData> transitions;
        public List<RuntimeServiceData> runtimeServices;

        public bool isRunning;

        public virtual void start()
        {
            isRunning = true;

            runtimeLayerData.CallNodeStarted(associatedNode);

            runtimeLayerData.CallStateStarted(associatedNode.state);

            if (runtimeStateData.associatedState)
            {
                runtimeStateData.associatedState.start(runtimeStateData);
            }

            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];
                    runtimeService.associatedService.onServiceStarted(runtimeService);
                    runtimeService.tickElapsedTime = runtimeService.tickTotalTime;
                }
            }
        }

        public RuntimeNodeData findRuntimeNodeByName(string stateName)
        {
            if (associatedNode.title == stateName)
            {
                return this;
            }

            if (this is RuntimeGroupData groupData)
            {
                foreach (var runtimeNodeData in groupData.nodes)
                {
                    var runtimeNode = runtimeNodeData.findRuntimeNodeByName(stateName);
                    if (runtimeNode != null)
                        return runtimeNode;
                }
            }

            return null;
        }

        public virtual StateResponse update(float dt)
        {
            // Debug.LogFormat("UPDATED NODE {0}", associatedNode.name);

            if (runtimeStateData.associatedState)
            {
                var result = runtimeStateData.associatedState.update(runtimeStateData, dt);
                if (result != StateResponse.Running)
                    return result;
            }

            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];

                    // update elapsed time
                    runtimeService.tickElapsedTime += dt;

                    // check time to tick
                    if (runtimeService.tickElapsedTime >= runtimeService.tickTotalTime)
                    {
                        // reset timer
                        runtimeService.tickElapsedTime = 0;

                        // tick
                        var result = runtimeService.associatedService.tick(runtimeService);

                        if (result != StateResponse.Running)
                            return result;
                    }
                }
            }

            return StateResponse.Running;
        }

        public virtual void end()
        {
            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];
                    runtimeService.associatedService.onServiceEnded(runtimeService);
                }
            }

            if (runtimeStateData.associatedState)
            {
                runtimeStateData.associatedState.end(runtimeStateData);
            }

            runtimeLayerData.CallStateEnded(associatedNode.state);

            runtimeLayerData.CallNodeEnded(associatedNode);

            isRunning = false;
        }

        public void broadcastTickServicesExcept(IService service)
        {
            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];

                    if (runtimeService.associatedService != service)
                        runtimeService.associatedService.tick(runtimeService);
                }
            }
        }

        public RuntimeTransitionData evaluateTransitions(bool hasExitTime)
        {
            RuntimeTransitionData maxti = null;
            float maxW = 0.0f;

            if (transitions != null)
            {
                for (int i = 0; i < transitions.Count; ++i)
                {
                    RuntimeTransitionData ti = transitions[i];

                    bool trueHasExitTime = ti.hasExitTime;
                    if (ti.hasExitTime && ti.exitTime < 1)
                    {
                        if (runtimeStateData.ratio >= ti.exitTime)
                        {
                            trueHasExitTime = false;
                        }
                    }

                    if (hasExitTime == trueHasExitTime)
                    {
                        if (evaluateTransition(ti))
                        {
                            if (maxW < ti.weight)
                            {
                                maxti = ti;
                                maxW = ti.weight;
                            }
                        }
                    }
                }
            }

            return maxti;
        }

        public bool evaluateTransition(RuntimeTransitionData transition)
        {
            bool finalValue = true;            

            for (int i = 0; i < transition.conditions.Count; ++i)
            {
                RuntimeConditionData cond = transition.conditions[i];

                bool value = cond.value.compare(cond);
                if (i > 0)
                {
                    if (cond.nextOperand == Operand.And)
                    {
                        finalValue = finalValue && value;
                        if (finalValue == false)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        finalValue = finalValue || value;
                    }
                }
                else
                {
                    finalValue = value;
                }
            }

            return finalValue;
        }
    }
}