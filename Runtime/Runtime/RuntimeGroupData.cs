using System.Collections.Generic;

namespace Behaviours.HFSM.Runtime
{
    public class RuntimeGroupData : RuntimeNodeData
    {
        public List<RuntimeNodeData> nodes = new List<RuntimeNodeData>();

        public RuntimeNodeData actualNode;
        public RuntimeNodeData anyNode;
        public RuntimeNodeData defaultNode;
        public RuntimeNodeData exitNode;

        public bool IsParentOf(RuntimeNodeData gd)
        {
            gd = this;

            while (gd != null)
            {
                if (gd == this)
                    return true;

                gd = gd.parentGroup;
            }

            return false;
        }        

        public override void start()
        {
            isRunning = true;

            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];
                    runtimeService.associatedService.onServiceStarted(runtimeService);
                }
            }

            if (defaultNode != null)
            {
                changeState(defaultNode);
                update(0);
            }
        }

        public override void end()
        {
            if (runtimeServices != null)
            {
                for (int i = 0, n = runtimeServices.Count; i < n; i++)
                {
                    var runtimeService = runtimeServices[i];
                    runtimeService.associatedService.onServiceEnded(runtimeService);
                }
            }

            if (actualNode != null)
            {
                actualNode.end(); 
            }

            isRunning = false;
        }

        StateResponse getStateResponceByNode(RuntimeNodeData actualNode)
        {
            if (actualNode == null)
                return StateResponse.Finished;

            if (actualNode == exitNode)
                return StateResponse.Finished;

            return StateResponse.Running;
        }

        public override StateResponse update(float dt)
        {
            if ((exitNode != null) && (actualNode == exitNode))
            {
                changeState(null);
                return StateResponse.Finished;
            }

            // CHECK TRANSITIONS FROM ANY NODE
            if (anyNode != null)
            {
                RuntimeTransitionData transition = anyNode.evaluateTransitions(false);
                if (transition != null)
                {
                    changeState(transition.node);

                    return getStateResponceByNode(actualNode);
                }
            }

            // CHECK OTHER TRANSITIONS
            if (actualNode != null)
            {
                // CHECK TRANSITIONS WITH NO EXIT TIME
                RuntimeTransitionData transition = actualNode.evaluateTransitions(false);
                if (transition != null)
                {
                    changeState(transition.node);

                    return getStateResponceByNode(actualNode);
                }
                else
                {
                    // UPDATE NODE
                    StateResponse r = actualNode.update(dt);
                    if (r == StateResponse.Finished)
                    {
                        // CHECK CHILD TRANSITIONS WITH EXIT TIME
                        transition = actualNode.evaluateTransitions(true);
                        if (transition != null)
                        {
                            changeState(transition.node);

                            return getStateResponceByNode(actualNode);
                        }
                    }
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

            return StateResponse.Finished;
        }

        public RuntimeNodeData changeState(RuntimeNodeData node)
        {
            if (actualNode != null)
            {
                actualNode.end();
            }            

            actualNode = node;

            if (actualNode != null)
            {
                actualNode.start();
            }

            runtimeLayerData.CurrentNode = actualNode;

            return actualNode;
        }
    }
}