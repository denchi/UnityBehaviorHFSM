using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Behaviours
{
    namespace HFSM
    {
        [System.Serializable]
        public class Transition : ScriptableObject
        {
            public Node node;

            public float weight;
            public float exitTime = 1;
            public bool hasExitTime = true;

            public List<Condition> conditions;
            public bool _editorFoldout = true;

            void OnEnable()
            {
                hideFlags = HideFlags.HideInHierarchy;

                if (conditions == null)
                {
                    conditions = new List<Condition>();
                }
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(this.name);
                writer.Write(this.weight);
                writer.Write(this.hasExitTime);

                int nodeIdx = (node.parent.state as IComposedState).nodes.IndexOf(node);

                writer.Write(nodeIdx);

                writer.Write(conditions.Count);
                foreach (var condition in conditions)
                {
                    condition.Write(writer);
                }
            }

            public bool HasValues(Value[] values, bool[] constants)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var constant = conditions.Find(x => x.value == values[i] && x.bConstant == constants[i]);
                    if (constant == null)
                        return false;
                }
                return true;
            }
        }
    }
}