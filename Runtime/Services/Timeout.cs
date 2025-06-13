using Behaviours.HFSM.Runtime;

namespace Behaviours.HFSM.Services
{
    [RuntimeData(typeof(ServiceData))]
    public class Timeout : IService
    {
        public float timeoutTimeMin = 1;
        public float timeoutTimeMax = 1;

        public class ServiceData : Runtime.RuntimeServiceData
        {
            public float elapsedTime = 0;
            public float timeoutTime = 0;
        }

        public override void onServiceStarted(RuntimeServiceData serviceData)
        {
            base.onServiceStarted(serviceData);

            ServiceData data = (ServiceData)serviceData;
            data.timeoutTime = UnityEngine.Random.Range(timeoutTimeMin, timeoutTimeMax);
            data.elapsedTime = data.timeoutTime;
        }

        public override StateResponse tick(RuntimeServiceData serviceData)
        {
            ServiceData data = (ServiceData)serviceData;
            data.elapsedTime += data.tickTotalTime;
            if (data.elapsedTime >= data.timeoutTime)
            {
                return StateResponse.Finished;
            }

            return base.tick(serviceData);
        }
    }
}