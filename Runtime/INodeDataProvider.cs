using System.Collections.Generic;

namespace Behaviours.HFSM.Runtime
{
    public interface INodeDataProvider
    {
        List<(string, NodeData)> GetNodeData(object value);
        void SetNodeData(CloneOptions options);
    }
}