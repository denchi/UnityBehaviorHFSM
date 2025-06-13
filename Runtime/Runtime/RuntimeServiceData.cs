namespace Behaviours.HFSM.Runtime
{
    public class RuntimeServiceData
    {
        public RuntimeNodeData runtimeNode;
        public IService associatedService;

        public float tickTotalTime = 1;
        public float tickElapsedTime = 0;

        public void Init(RuntimeNodeData runtimeNode, IService associatedService)
        {
            this.runtimeNode = runtimeNode;
            this.associatedService = associatedService;
            tickTotalTime = 1.0f / associatedService.ticksPerSecond;
        }

        public T GetService<T>()
        {
            return runtimeNode.runtimeLayerData.GetService<T>();
        }
    }
}