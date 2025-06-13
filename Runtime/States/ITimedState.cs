using UnityEngine;
using Behaviours.HFSM.Runtime;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Behaviours
{
    namespace HFSM
    {
        public class ITimedState : IBaseState
        {
            public float duration;
            public bool loop;

            public class RuntimeTimeState : RuntimeStateData
            {
                public float elapsed;                
            }

            public override RuntimeStateData createRuntimeData(Runtime.RuntimeLayerData layerData)
            {
                return new RuntimeTimeState();
            }

            public override void start(RuntimeStateData runtimeData)
            {
                base.start(runtimeData);

                RuntimeTimeState myRuntimeData = runtimeData as RuntimeTimeState;
                {
                    myRuntimeData.elapsed = 0.0f;
                    myRuntimeData.ratio = 0.0f;
                }
            }

            public override StateResponse update(RuntimeStateData runtimeData, float dt)
            {
                RuntimeTimeState myRuntimeData = runtimeData as RuntimeTimeState;
                {
                    myRuntimeData.elapsed += dt;
                    myRuntimeData.ratio = Mathf.Clamp01(myRuntimeData.elapsed / duration);
                }                

                if (myRuntimeData.ratio == 1)
                {
                    if (loop)
                    {
                        start(runtimeData);
                    }
                    else
                    {
                        return StateResponse.Finished;
                    }
                }

                return StateResponse.Running;
            }

            public override JObject serialize()
            {
                JObject jc = base.serialize();
                {
                    jc["loop"] = new JValue(loop);
                    jc["duration"] = new JValue(duration);
                }
                return jc;
            }

            public override void deserialize(JObject jc)
            {
                base.deserialize(jc);

                loop = jc["loop"].ToObject<bool>();
                duration = jc["duration"].ToObject<float>();
            }


            public override void Write(BinaryWriter writer)
            {
                base.Write(writer);

                writer.Write(this.duration);
                writer.Write(this.loop);
            }
        }
    }
}