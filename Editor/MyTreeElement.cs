using System;
using System.Reflection;


namespace Behaviours.HFSM.Editor
{
    public enum ChangeType
    {
        Unchanged = 0,
        Added,
        Removed,
        Modified,
    }

    public enum TargetType
    {
        Node = 0,
        State,
        Transition,
        Condition,
        Value,
        Field,
    }

    [Serializable]
	internal class MyTreeElement : TreeElement
	{
        public ChangeType changeType;
        public TargetType targetType;
        public string path;

        public object sourceObject;
        public object destinationObject;

        public FieldInfo sourceField;
        public FieldInfo destinationField;

        public bool enabled;

        public static int LastId = 100;
        public static int NextId { get { return ++LastId; } }

        public MyTreeElement (object source, object target, string path, ChangeType changeType, TargetType targetType, string name, int depth, int id) : base (name, depth, id)
		{
            this.changeType = changeType;
            this.targetType = targetType;
            this.path = path;
            this.sourceObject = source;
            this.destinationObject = target;

            this.enabled = true;
		}
	}
}
