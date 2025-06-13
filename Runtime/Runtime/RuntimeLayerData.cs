using System.Collections.Generic;
using Behaviours.Runtime;
using UnityEngine;

namespace Behaviours.HFSM.Runtime
{
    public class RuntimeLayerData
    {
        public RuntimeValueCollection runtimeValues;
        public RuntimeGroupData runtimeRootGroupData;
        public GameObject gameObject;
        public Layer associatedLayer;
        public RuntimeNodeData currentRuntimeNodeData;

        private RuntimeNodeData _currentNode;        

        public RuntimeNodeData CurrentNode
        {
            get => _currentNode;
            set
            {
                if (value == null)
                    return;

                if (value is RuntimeGroupData)
                    return;

                if (!value.associatedNode.parent)
                    return;

                if (!value.runtimeStateData.associatedState)
                    return;
                    
                currentRuntimeNodeData = value;
                CallNodeChanged(value);
                _currentNode = value;
            }
        }

        public void Init(GameObject gameObject, Layer layer)
        {
            this.gameObject = gameObject;
            associatedLayer = layer;
            runtimeValues = new RuntimeValueCollection();
            {
                runtimeValues.InitWithLayer(layer);
            }            
        }

        public void Start()
        {
            runtimeRootGroupData.start();
        }

        public void End()
        {
            runtimeRootGroupData.end();
        }

        public void Update(float dt)
        {
            runtimeRootGroupData.update(dt);
        }
        
        public event SpineAnimationDelegate OnSpineAnimationStarted;
        public event StateDelegate OnStateStarted;
        public event StateDelegate OnStateEnded;
        public event NodeDelegate OnNodeStarted;
        public event NodeDelegate OnNodeEnded;
        public event RuntimeNodeDelegate OnRuntimeNodeChanged;

        public delegate void SpineAnimationDelegate(string animationName, float animationSpeed, float animationStart);
        public delegate void StateDelegate(IBaseState state);
        public delegate void NodeDelegate(Node node);
        public delegate void RuntimeNodeDelegate(RuntimeNodeData node);

        public void CallSpineAnimationStarted(string animationName, float animationSpeed, float animationStart)
        {
            if (OnSpineAnimationStarted != null)
                OnSpineAnimationStarted(animationName, animationSpeed, animationStart);
        }
        
        public void CallStateStarted(IBaseState state)
        {
            if (OnStateStarted != null)
                OnStateStarted(state);
        }

        public void CallStateEnded(IBaseState state)
        {
            if (OnStateEnded != null)
                OnStateEnded(state);
        }

        public void CallNodeStarted(Node node)
        {
            if (OnNodeStarted != null)
                OnNodeStarted(node);
        }

        public void CallNodeEnded(Node node)
        {
            if (OnNodeEnded != null)
                OnNodeEnded(node);
        }

        public void CallNodeChanged(RuntimeNodeData node)
        {
            if (OnRuntimeNodeChanged != null)
                OnRuntimeNodeChanged(node);
        }


        #region Services

        private readonly List<object> _servicesValues = new ();

        public T GetService<T>()
        {
            var value = (T) _servicesValues.Find(x => x is T);
            Debug.Assert(value != null, $"Can not find service of type {typeof(T)} in {gameObject} [{gameObject.GetInstanceID()}]");
            return value;
        }
        
        public void SetService<T>(T value)
        {
            if (value == null)
                return;
            
            var index = _servicesValues.FindIndex(x => x is T);
            if (index == -1)
                _servicesValues.Add(value);
            else
                _servicesValues[index] = value;
        }

        public void AddService(object value)
        {
            if (value == null)
                return;
            
            var index = _servicesValues.FindIndex(x => x == value);
            if (index == -1)
                _servicesValues.Add(value);
            else
                _servicesValues[index] = value;
        }

        public IReadOnlyCollection<object> GetServices() => _servicesValues;

        #endregion
    }
}