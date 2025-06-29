using System.IO;
using UnityEngine;

namespace Behaviours
{
    namespace HFSM
    {
        public class Layer : ILayer
        {
            public Node root;

            public IComposedState composedState
            {
                get
                {
                    if (root)
                    {
                        if (root.state)
                            return root.state as IComposedState;
                    }
                    return null;
                }
            }

            public Node findNode(string path)
            {
                var parts = path.Split(new char[] { '/' });

                var temp = root;

                var state = temp.state as IComposedState;
                for (var i = 0; i < parts.Length; i++)
                {
                    if (state != null)
                    {
                        temp = state.FindNodeByTitle(parts[i]);
                        if (temp != null)
                        {
                            state = temp.state as IComposedState;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                return temp;
            }

            public override void Write(BinaryWriter writer)
            {
                base.Write(writer);

                root.Write(writer);
            }
        }
    }
}