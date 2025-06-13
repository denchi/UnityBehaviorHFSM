using System.Collections.Generic;
using Behaviours.HFSM.Runtime;

namespace Behaviours.HFSM.Runtime
{
    [System.Serializable]
    public class CloneOptions
    {
        public bool cloneEvents = true;
        public bool cloneAnimations = true;
        public object sourceObject;
        public object destinationObject;
        public List<string> excludeNodes;

        [System.NonSerialized]
        public Dictionary<string, NodeData> replaceData;
        
        private static void ParseEntity(object entity, Dictionary<string, NodeData> replaceData)
        {
            if (entity is Node)
            {
                var node = entity as Node;
                ParseEntity(node.state, replaceData);
            }
            
            else if (entity is IComposedState composedState)
            {
                foreach (var node in composedState.nodes)
                {
                    ParseEntity(node, replaceData);
                }
            }
                
            else if (entity is INodeDataProvider nodeProvider)
            {
                foreach (var (path, nodeData) in nodeProvider.GetNodeData(entity))
                {
                    replaceData[path] = nodeData;    
                }
            }
            
            // else if (entity is SpineState spineState)
            // {
            //     var path = GetPath(spineState);
            //     replaceData[path] = new NodeData()
            //     {
            //         animationName = spineState.animationName,
            //         animationSpeed = spineState.animationSpeed,
            //         animationStart = spineState.animationStart,
            //         animationEnd = spineState.animationEnd,
            //         animationDuration = spineState.duration,
            //         animationTrack = spineState.animationTrack,
            //         animationEvents = spineState.events,
            //     };
            // }
            
            // else if (entity is ConditionalSpineState conditionalSpineState)
            // {
            //     var path = GetPath(conditionalSpineState);
            //
            //     foreach (var option in conditionalSpineState.options)
            //     {
            //         var token = string.Join("", option.conditions.Select(x => x.ToToken));
            //             
            //         // var token = "";
            //         // option.conditions.ForEach(x => token += x.ToToken);
            //
            //         var newPath = path + $"{token}";
            //
            //         replaceData[newPath] = new NodeData()
            //         {
            //             animationName = option.animation.animationName,
            //             animationSpeed = option.animation.animationSpeed,
            //             animationStart = option.animation.animationStart,
            //             animationEnd = option.animation.animationEnd,
            //             animationDuration = option.animation.duration,
            //             animationTrack = option.animation.animationTrack,
            //             animationEvents = option.animation.events,
            //         };
            //     }
            // }
        }
        
        public static CloneOptions WithReplacementData(object src, object dst, bool cloneEvents, bool cloneAnimations)
        {
            CloneOptions options = new CloneOptions();
            options.cloneEvents = cloneEvents;
            options.cloneAnimations = cloneAnimations;
            options.sourceObject = src;
            options.destinationObject = dst;
            options.replaceData = new Dictionary<string, NodeData>();
            ParseEntity(dst, options.replaceData);
            return options;
        }
        
        public static CloneOptions WithOptions(object src, object dst, CloneOptions options)
        {
            options.sourceObject = src;
            options.destinationObject = dst;
            options.replaceData = new Dictionary<string, NodeData>();
            ParseEntity(dst, options.replaceData);
            return options;
        }
        
        public static CloneOptions Default => new CloneOptions();
        
        public static string GetPath(object target)
        {
            string path = "";
        
            while (target != null)
            {
                if (target is Node)
                {
                    var node = target as Node;
                    path = node.title + "/" + path;
                    target = node.parent ? node.parent.state : null;
                }
                else if (target is IBaseState)
                {
                    var state = target as IBaseState;
                    target = state.node;
                }
                else
                {
                    target = null;
                }
            }
        
            return "/" + path;
        
        }
    }
}