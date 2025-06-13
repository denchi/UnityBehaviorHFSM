using Behaviours.HFSM.Runtime;
using UnityEngine;

namespace Behaviours.HFSM
{
    public class Animator : MonoBehaviour
    {
        public Layer layer;
        public bool playOnAwake = true;
        public RuntimeLayerData runtimeLayer;

        [HideInInspector]
        public bool isInitialized;

        public delegate void InitializedCallback();
        public event InitializedCallback onInitialized;

        bool isInitializing;
        bool isPaused;
        bool isRunning;

        private void Start()
        {
            if (!playOnAwake) 
                return;
            
            if (layer != null && isInitialized == false && isInitializing == false)
            {
                Debug.Log($"Initializing layer: {gameObject.name}");
                SetLayer(layer);
            }
        }

        private void SetLayer(Layer layer)
        {
            this.layer = layer;

            isRunning = false;
            isPaused = false;
            isInitialized = false;
            isInitializing = true;
            
            runtimeLayer = Utils.CreateWithLayer(layer, gameObject);
            isInitialized = true;
            isInitializing = false;
            onInitialized?.Invoke();
        }

        private void OnValidate()
        {
            if (layer != null)
            {
                layer.gameObject = gameObject;
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (!isInitialized) 
                return;
            
            // if not started them start
            if (isRunning == false)
            {
                runtimeLayer.Start();
                isRunning = true;
            }
            else
            {
                // update only if not paused
                if (isPaused == false)
                {
                    runtimeLayer.Update(Time.deltaTime);
                }
            }
        }

        public void Pause()
        {
            if (isInitialized)
                isPaused = true;
        }

        public void Resume()
        {
            if (isInitialized)
                isPaused = false;
        }

        public void Restart()
        {
            if (!isInitialized) 
                return;
            
            isRunning = true;
            isPaused = false;

            runtimeLayer.End();
            runtimeLayer.Start();
        }

        public void Play(string path)
        {
            runtimeLayer.End();

            var parts = path.Split(new[] { '/' });
            RuntimeNodeData temp = runtimeLayer.runtimeRootGroupData;
            for (var i = 0; i < parts.Length; i++)
            {
                temp = temp.findRuntimeNodeByName(parts[i]);
                if (temp != null)
                {
                    temp.start();
                }
                else
                {
                    runtimeLayer.Start();
                    Debug.LogErrorFormat("Changing node to {0}:{1} failed. {1} not found!", i, parts[i]);
                    break;
                }
            }
        }

        #region SETTER NAMES

        public void SetFloat(string name, float value)
        {
            if (isInitialized)
                runtimeLayer.runtimeValues.SetFloat(name, value);
        }

        public void SetInt(string name, int value)
        {
            if (isInitialized)
                runtimeLayer.runtimeValues.SetInt(name, value);
        }

        public void SetBool(string name, bool value)
        {
            if (isInitialized)
                runtimeLayer.runtimeValues.SetBool(name, value);
        }

        public void SetString(string name, string value)
        {
            if (isInitialized)
                runtimeLayer.runtimeValues.SetString(name, value);
        }

        public void SetTrigger(string name)
        {
            if (isInitialized)
                runtimeLayer.runtimeValues.SetTrigger(name, this);
        }

        #endregion

        #region GETTER NAMES

        public float GetFloat(string name)
        {
            return runtimeLayer.runtimeValues.GetFloat(name);
        }

        public int GetInt(string name)
        {
            return runtimeLayer.runtimeValues.GetInt(name);
        }

        public bool GetBool(string name)
        {
            return runtimeLayer.runtimeValues.GetBool(name);
        }

        public string GetString(string name)
        {
            return runtimeLayer.runtimeValues.GetString(name);
        }

        #endregion

        #region SETTER HASHCODE

        public void SetFloat(int hashCode, float value)
        {
            runtimeLayer.runtimeValues.SetFloat(hashCode, value);
        }

        public void SetInt(int hashCode, int value)
        {
            runtimeLayer.runtimeValues.SetInt(hashCode, value);
        }

        public void SetBool(int hashCode, bool value)
        {
            runtimeLayer.runtimeValues.SetBool(hashCode, value);
        }

        public void SetString(int hashCode, string value)
        {
            runtimeLayer.runtimeValues.SetString(hashCode, value);
        }

        public void SetTrigger(int hashCode)
        {
            runtimeLayer.runtimeValues.SetTrigger(hashCode, this);
        }

        #endregion

        #region GETTER HASHCODE

        public float GetFloat(int hashCode)
        {
            return runtimeLayer.runtimeValues.GetFloat(hashCode);
        }

        public int GetInt(int hashCode)
        {
            return runtimeLayer.runtimeValues.GetInt(hashCode);
        }

        public bool GetBool(int hashCode)
        {
            return runtimeLayer.runtimeValues.GetBool(hashCode);
        }

        public string GetString(int hashCode)
        {
            return runtimeLayer.runtimeValues.GetString(hashCode);
        }

        #endregion
    }
}