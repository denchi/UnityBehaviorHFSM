using System.Collections.Generic;

using Behaviours.Runtime;

namespace Behaviours.HFSM.Runtime
{
    public class RuntimeTransitionData
    {
        public RuntimeNodeData node;

        public float weight;
        public float exitTime = 1;
        public bool hasExitTime = true;

        public List<RuntimeConditionData> conditions;

        void OnEnable()
        {
            if (conditions == null)
            {
                conditions = new List<RuntimeConditionData>();
            }
        }
    }    
}