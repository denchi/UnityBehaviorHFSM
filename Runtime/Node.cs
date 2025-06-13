using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Behaviours
{
    namespace HFSM
    {
        public class Node : ScriptableObject
        {
            public IBaseState state;
            public List<Transition> transitions;
            public List<IService> services;

            public Rect rect;

            public Layer layer;
            public Node parent;

            public string title;

            [System.NonSerialized]
            public int hashCode = -1;

            [System.NonSerialized]
            public bool initialized = false;

            public virtual void OnEnable()
            {
                hideFlags = HideFlags.HideInHierarchy;

                if (transitions == null)
                {
                    transitions = new List<Transition>();
                }

                if (services == null)
                {
                    services = new List<IService>();
                }

                if (string.IsNullOrEmpty(title))
                {
                    title = "New Node";
                }

                hashCode = title.GetHashCode();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(this.title);

                writer.Write(this.rect.x);
                writer.Write(this.rect.y);
                writer.Write(this.rect.width);
                writer.Write(this.rect.height);

                if (this.state != null)
                {
                    var stateTypeName = state.GetType().FullName.Replace("Behaviours.HFSM.", "");
                    writer.Write(stateTypeName);
                    this.state.Write(writer);
                }
                else
                {
                    writer.Write("");
                }

                writer.Write(transitions.Count);
                foreach (var transition in transitions)
                {
                    transition.Write(writer);
                }
            }

            public string getPath()
            {
                var temp = this;
                var path = this.title;
                while (temp.parent)
                {
                    path = temp.parent.title + "/" + path;
                    temp = temp.parent;
                }
                return path;
            }

            public string getSimplifiedPath()
            {
                var temp = this;
                var path = this.title;
                while (temp.layer && temp.parent && temp.parent != temp.layer.root)
                {
                    path = temp.parent.title + "/" + path;
                    temp = temp.parent;
                }
                return path;
            }

            public T GetStateAs<T>() where T : IBaseState
            {
                return state as T;
            }
        }     
    }
}