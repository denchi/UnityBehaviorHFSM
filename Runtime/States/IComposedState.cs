using System.Collections.Generic;
using System.IO;

namespace Behaviours
{
    namespace HFSM
    {
        public class IComposedState : IBaseState
        {
            public List<Node> nodes;

            public int anyNodeIndex = -1;
            public int defaultNodeIndex = -1;
            public int exitNodeIndex = -1;            

            public Node FindNodeByIdx(int idx)
            {
                return (idx >= 0 && nodes != null && idx < nodes.Count) ? nodes[idx] : null;
            }

            public Node FindNodeByTitle(string title)
            {
                return nodes.Find(x => x.title.Equals(title));
            }

            public Node GetDefaultNode()
            {
                return nodes[defaultNodeIndex];
            }

            public Node GetAnyNode()
            {
                return nodes[anyNodeIndex];
            }

            public Node GetExitNode()
            {
                return nodes[exitNodeIndex];
            }

            public T FindStateByNodeTitle<T>(string title) where T : IBaseState
            {
                var node = FindNodeByTitle(title);
                if (node != null)
                {
                    return node.state as T;
                }
                return null;
            }

            public int FindIdxByNode(Node node)
            {
                if (node)
                    return nodes.IndexOf(node);
                return -1;
            }

            public override void OnEnable()
            {
                base.OnEnable();

                if (nodes == null)
                {
                    nodes = new List<Node>();
                }
            }

            public override void Write(BinaryWriter writer)
            {
                base.Write(writer);

                writer.Write(this.anyNodeIndex);
                writer.Write(this.defaultNodeIndex);
                writer.Write(this.exitNodeIndex);

                int nodesCnt = nodes.Count;
                writer.Write(nodesCnt);
                for (int i = 0; i < nodesCnt; i++)
                {
                    // create node
                    var childNode = nodes[i];

                    // write node
                    childNode.Write(writer);
                }
            }
        }
    }
}