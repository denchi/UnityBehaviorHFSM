using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Behaviours
{
    namespace HFSM
    {
        public class IBaseState : ScriptableObject
        {
            public Node node;

            [HideInInspector]
            public bool _editorOnlyExpanded;

            public virtual void OnEnable()
            {
                hideFlags = HideFlags.HideInHierarchy;
            }


            // Creates data holder for specific object
            public virtual Runtime.RuntimeStateData createRuntimeData(Runtime.RuntimeLayerData layerData)
            {
                return new Runtime.RuntimeStateData();
            }

            // Executed whenever this state starts
            public virtual void start(Runtime.RuntimeStateData runtimeData)
            {
                runtimeData.ratio = 1;
            }

            // Executed whenever this state updates
            public virtual StateResponse update(Runtime.RuntimeStateData runtimeData, float dt)
            {
                return StateResponse.Finished;
            }

            // Executed whenever this state ends
            public virtual void end(Runtime.RuntimeStateData runtimeData)
            {

            }


            // SERIALIZATION : JSON

            public virtual JObject serialize()
            {
                var jc = new JObject();
                {
                    jc["type"] = GetType().FullName;
                }
                return jc;
            }

            public virtual void deserialize(JObject jc)
            {

            }

            // SERIALIZATION : BINARY

            public virtual void Write(BinaryWriter writer)
            {
                writer.Write(this.name);
            }

            public string getPath()
            {
                var temp = node;
                var path = node.title;
                while (temp.parent)
                {
                    path = temp.parent.title + "/" + path;
                    temp = temp.parent;
                }
                return path;
            }
        }
    }
}