using UnityEngine;

namespace Behaviours.HFSM
{
    /// <summary>
    /// A service is a code running while it's node is running
    /// </summary>
    public class IService : ScriptableObject
    {
        public Node node;

        // how many ticks per second
        public int ticksPerSecond = 1;

        [HideInInspector]
        public bool _editorOnlyExpanded = true;

        private void OnEnable()
        {
            hideFlags = HideFlags.HideInHierarchy;
        }

        public Runtime.RuntimeServiceData createRuntimeServiceData(Runtime.RuntimeNodeData runtimeNode)
        {
            var runtimeService = RuntimeDataAttribute.createRuntimeDataFromAttribute<Runtime.RuntimeServiceData>(GetType());
            if (runtimeService == null)
            {
                runtimeService = new Runtime.RuntimeServiceData();
            }

            runtimeService.Init(runtimeNode, this);

            return runtimeService;
        }       

        public virtual void onServiceStarted(Runtime.RuntimeServiceData serviceData)
        {
            serviceData.tickElapsedTime = 0;
        }

        public virtual StateResponse tick(Runtime.RuntimeServiceData serviceData)
        {
            return StateResponse.Running;
        }

        public virtual void onServiceEnded(Runtime.RuntimeServiceData serviceData)
        {

        }        
    }
}