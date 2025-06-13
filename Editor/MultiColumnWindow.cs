using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Behaviours.HFSM.Editor
{
    class MultiColumnWindow : EditorWindow
    {
        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        SearchField m_SearchField;
        MultiColumnTreeView m_TreeView;

        [System.NonSerialized] List<MyTreeElement> _TreeElements;
        internal List<MyTreeElement> treeElements
        {
            get
            {
                //if (m_TreeElements == null)
                //{
                //    m_TreeElements = new List<MyTreeElement>();
                //    m_TreeElements.Add(new MyTreeElement(null, null, "", ChangeType.Unchanged, TargetType.Node, "Root", -1, 0));
                //}

                return _TreeElements;
            }
            //set { m_TreeElements = value; }
        }

        [MenuItem("Window/Multi Columns")]
        public static MultiColumnWindow GetWindow()
        {
            var window = GetWindow<MultiColumnWindow>();
            window.titleContent = new GUIContent("Multi Columns");
            window.Focus();
            window.Repaint();
            return window;
        }

        const System.Reflection.BindingFlags FLAGS = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        public static System.Reflection.FieldInfo TryGetField(object obj, string name)
        {
            if (obj == null)
                return null;

            try
            {
                return obj.GetType().GetField(name, FLAGS);
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public static bool HasField(object obj, string name)
        {
            if (obj == null)
                return false;

            try
            {
                return obj.GetType().GetField(name, FLAGS) != null;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static List<System.Reflection.FieldInfo> TryGetFields(object obj)
        {
            if (obj == null)
                return new List<System.Reflection.FieldInfo>();

            try
            {
                var itms = obj.GetType().GetFields(FLAGS);
                if (itms == null)
                    return new List<System.Reflection.FieldInfo>();

                var l = new List<System.Reflection.FieldInfo>();
                foreach (var f in itms)
                {
                    if (f.Name.Equals("parent"))
                        continue;

                    if (f.Name.Equals("rect"))
                        continue;

                    if (f.Name.Equals("layer"))
                        continue;

                    if (f.Name.Equals("node"))
                        continue;

                    if (f.Name.Equals("hashCode"))
                        continue;

                    if (f.Name.Equals("initialized"))
                        continue;

                    if (f.Name.Equals("_editorFoldout"))
                        continue;

                    //if (f.Name.Equals("value") && (obj is Condition))
                    //    continue;

                    l.Add(f);
                }
                return l;
            }
            catch (Exception ex)
            {
                return new List<System.Reflection.FieldInfo>();
            }
        }

        public struct FieldPair
        {
            public System.Reflection.FieldInfo sourceField;
            public System.Reflection.FieldInfo destinationField;
            public object sourceObject;
            public object destinationObject;
        }

        TargetType GetTypeFromObj(object obj)
        {
            if (obj is Node)
                return TargetType.Node;
            else if (obj is IBaseState)
                return TargetType.State;
            else if (obj is Transition)
                return TargetType.Transition;
            else if (obj is Condition)
                return TargetType.Condition;
            else if (obj is Value)
                return TargetType.Value;
            else
                return TargetType.Field;
        }

        ChangeType GetChangeTypeFromObjects(object obj1, object obj2)
        {
            if (obj1.GetType().IsClass)
            {
                return obj1.GetType().Equals(obj2.GetType()) ? ChangeType.Unchanged : ChangeType.Modified;
            }

            return obj1.Equals(obj2) ? ChangeType.Unchanged : ChangeType.Modified;
        }

        public MyTreeElement parseObject(object src, object dst, MyTreeElement parent, string fieldName)
        {
            MyTreeElement obj = null;

            if (src != null && dst != null)
            {
                var list = new List<FieldPair>();

                var srcFields = TryGetFields(src);
                foreach (var srcField in srcFields)
                {
                    var dstField = TryGetField(dst, srcField.Name);
                    if (dstField != null)
                    {
                        list.Add(new FieldPair() { sourceObject = src, sourceField = srcField, destinationObject = dst, destinationField = dstField });
                    }
                    else
                    {
                        list.Add(new FieldPair() { sourceObject = src, sourceField = srcField, destinationObject = dst, destinationField = null });
                    }
                }

                var dstFields = TryGetFields(dst);
                var missingFields = dstFields.FindAll(x => !HasField(src, x.Name));
                foreach (var dstField in missingFields)
                {
                    list.Add(new FieldPair() { sourceObject = src, sourceField = null, destinationObject = dst, destinationField = dstField });
                }

                // ADD TREE ELEMENT
                var ct = GetChangeTypeFromObjects(src, dst);
                obj = (MyTreeElement)parent.AddChild(new MyTreeElement(src, dst, GetPath(parent, fieldName), ct, GetTypeFromObj(src), GetName(src, dst, fieldName, ct), parent.depth + 1, MyTreeElement.NextId));

                if (src.GetType().IsClass && (src.GetType() == dst.GetType()))
                    parseFields(src, dst, list, obj);
            }
            else if (src != null)
            {
                // ADD TREE ELEMENT
                obj = (MyTreeElement)parent.AddChild(new MyTreeElement(src, dst, GetPath(parent, fieldName), ChangeType.Removed, GetTypeFromObj(src), GetName(src, dst, fieldName, ChangeType.Removed), parent.depth + 1, MyTreeElement.NextId));
            }
            else if (dst != null)
            {
                // ADD TREE ELEMENT
                obj = (MyTreeElement)parent.AddChild(new MyTreeElement(src, dst, GetPath(parent, fieldName), ChangeType.Added, GetTypeFromObj(dst), GetName(src, dst, fieldName, ChangeType.Added), parent.depth + 1, MyTreeElement.NextId));
            }
            return obj;
        }

        private static string GetName(object src, object dst, string fieldName, ChangeType type)
        {
            if (type == ChangeType.Modified && src is IBaseState)
            {
                var n = src.GetType().Name + "->" + dst.GetType().Name;
                return $"{fieldName}:{n}";
            }
            else
            {
                var n = src != null ? src.GetType().Name : dst.GetType().Name;
                return $"{fieldName}:{n}";
            }
        }

        private static string GetPath(MyTreeElement parent, string fieldName)
        {
            return (parent.path != null ? parent.path : "") + "/" + fieldName;
        }

        public void parseFields(object srcObj, object dstObj, List<FieldPair> fields, MyTreeElement parent)
        {
            foreach (var pair in fields)
            {
                var src = pair.sourceField;
                var dst = pair.destinationField;

                var fieldName = pair.sourceField != null ? pair.sourceField.Name : pair.destinationField.Name;

                var child = parseObject(src != null ? src.GetValue(srcObj) : null, dst != null ? dst.GetValue(dstObj) : null, parent, fieldName);

                if (src != null && dst != null)
                {
                    if (src.FieldType.IsArray)
                    {
                        var srcArr = (Array)src.GetValue(srcObj);
                        var dstArr = (Array)dst.GetValue(dstObj);
                        for (var i = 0; i < Math.Max(srcArr.Length, dstArr.Length); i++)
                        {
                            var srcArrayElement = srcArr.Length > i ? srcArr.GetValue(i) : null;
                            var dstArrayElement = dstArr.Length > i ? dstArr.GetValue(i) : null;

                            parseObject(srcArrayElement, dstArrayElement, child, $"[{i}]");
                        }
                    }
                    else if (src.FieldType.IsGenericType)
                    {
                        var srcArr = ((IEnumerable)src.GetValue(srcObj)).Cast<object>();
                        var dstArr = ((IEnumerable)dst.GetValue(dstObj)).Cast<object>();
                        for (var i = 0; i < Math.Max(srcArr.Count(), dstArr.Count()); i++)
                        {
                            var srcArrayElement = srcArr.Count() > i ? srcArr.ElementAt(i) : null;
                            var dstArrayElement = dstArr.Count() > i ? dstArr.ElementAt(i) : null;

                            parseObject(srcArrayElement, dstArrayElement, child, $"[{i}]");
                        }
                    }
                }
                else if (src != null)
                {
                    if (src.FieldType.IsArray)
                    {
                        var srcArr = (Array)src.GetValue(srcObj);
                        for (var i = 0; i < srcArr.Length; i++)
                        {
                            var srcArrayElement = srcArr.GetValue(i);

                            parseObject(srcArrayElement, null, child, $"[{i}]");
                        }
                    }
                    else if (src.FieldType.IsGenericType)
                    {
                        var srcArr = ((IEnumerable)src.GetValue(srcObj)).Cast<object>();
                        for (var i = 0; i < srcArr.Count(); i++)
                        {
                            var srcArrayElement = srcArr.ElementAt(i);

                            parseObject(srcArrayElement, null, child, $"[{i}]");
                        }
                    }
                }
                else if (dst != null)
                {
                    if (dst.FieldType.IsArray)
                    {
                        var dstArr = (Array)dst.GetValue(dstObj);
                        for (var i = 0; i < dstArr.Length; i++)
                        {
                            var dstArrayElement = dstArr.GetValue(i);

                            parseObject(null, dstArrayElement, child, fieldName + $"[{i}]");
                        }
                    }
                    else if (dst.FieldType.IsGenericType)
                    {
                        var dstArr = ((IEnumerable)dst.GetValue(dstObj)).Cast<object>();
                        for (var i = 0; i < dstArr.Count(); i++)
                        {
                            var dstArrayElement = dstArr.ElementAt(i);

                            parseObject(null, dstArrayElement, child, $"[{i}]");
                        }
                    }
                }
            }
        }

        HFSM.Layer srcLayer;
        HFSM.Layer dstLayer;

        public void SetNodes(Node src, Node dst)
        {
            srcLayer = src.layer;
            dstLayer = dst.layer;

            var e = new MyTreeElement(null, null, null, ChangeType.Unchanged, TargetType.Node, "Root", -1, MyTreeElement.NextId);
            parseObject(src, dst, e, "root");

            var listToDelete = new List<MyTreeElement>();
            CollectUnchanged(e, listToDelete);
            foreach (var element in listToDelete)
            {
                if (element.parent != null)
                {
                    var was = element.parent.children.Count;
                    element.parent.children.Remove(element);
                    var now = element.parent.children.Count;
                    //Debug.Log($"Removed {element.path} was: {was} now: {now}");
                }
            }

            _TreeElements = new List<MyTreeElement>();
            _TreeElements.Add(e);
        }

        //public static void CollectUnchanged(MyTreeElement element, List<MyTreeElement> unchanged)
        //{
        //    if (element.changeType == ChangeType.Unchanged)
        //        unchanged.Add(element);

        //    if (element.children != null)
        //        element.children.ForEach(child => CollectUnchanged((MyTreeElement) child, unchanged));
        //}

        public static void CollectUnchanged(MyTreeElement element, List<MyTreeElement> elementsToDelete)
        {
            if (!DidChange(element))
                elementsToDelete.Add(element);
            else if (element.children != null)
                element.children.ForEach(child => CollectUnchanged((MyTreeElement)child, elementsToDelete));
        }

        public static bool DidChange(MyTreeElement element)
        {
            if (element.changeType == ChangeType.Unchanged)
            {
                //if (element.path != null && element.path.Equals("/root/state/nodes/[0]/transitions/[1]/conditions/[0]/sConstant"))
                //{
                //    Debug.Log($"Found {element.id}");
                //}

                if (element.children != null)
                {
                    foreach (var child in element.children)
                    {
                        if (DidChange((MyTreeElement)child))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        public static MultiColumnWindow GetWindow(Node src, Node dest)
        {
            var window = GetWindow<MultiColumnWindow>();

            window.SetNodes(src, dest);
            window.m_Initialized = false;
            window.InitIfNeeded();

            window.titleContent = new GUIContent("Multi Columns");
            window.Focus();
            window.Repaint();

            return window;
        }

        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 30, position.width - 40, position.height - 60); }
        }

        Rect toolbarRect
        {
            get { return new Rect(20f, 10f, position.width - 40f, 20f); }
        }

        Rect bottomToolbarRect
        {
            get { return new Rect(20f, position.height - 18f, position.width - 40f, 16f); }
        }

        public MultiColumnTreeView treeView
        {
            get { return m_TreeView; }
        }

        void InitIfNeeded()
        {
            if (!m_Initialized)
            {
                if (treeElements == null || (treeElements != null && treeElements.Count == 0))
                    return;

                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                bool firstInit = m_MultiColumnHeaderState == null;
                var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;

                var multiColumnHeader = new MyMultiColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                var treeModel = new TreeModel<MyTreeElement>(GetData());

                m_TreeView = new MultiColumnTreeView(m_TreeViewState, multiColumnHeader, treeModel);

                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                m_Initialized = true;
            }
        }

        List<MyTreeElement> GetData()
        {
            return treeElements;
        }

        void OnSelectionChange()
        {
            if (!m_Initialized)
                return;
        }

        void OnGUI()
        {
            InitIfNeeded();

            if (!m_Initialized)
                return;

            SearchBar(toolbarRect);
            DoTreeView(multiColumnTreeViewRect);
            BottomToolBar(bottomToolbarRect);
        }

        void SearchBar(Rect rect)
        {
            treeView.searchString = m_SearchField.OnGUI(rect, treeView.searchString);
        }

        void DoTreeView(Rect rect)
        {
            m_TreeView.OnGUI(rect);
        }

        void BottomToolBar(Rect rect)
        {
            GUILayout.BeginArea(rect);

            using (new EditorGUILayout.HorizontalScope())
            {

                var style = "miniButton";
                if (GUILayout.Button("Expand All", style))
                {
                    treeView.ExpandAll();
                }

                if (GUILayout.Button("Collapse All", style))
                {
                    treeView.CollapseAll();
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label("Tree");

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Set sorting", style))
                {
                    var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                    myColumnHeader.SetSortingColumns(new int[] { 4, 3, 2 }, new[] { true, false, true });
                    myColumnHeader.mode = MyMultiColumnHeader.Mode.LargeHeader;
                }


                GUILayout.Label("Header: ", "minilabel");
                if (GUILayout.Button("Large", style))
                {
                    var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                    myColumnHeader.mode = MyMultiColumnHeader.Mode.LargeHeader;
                }
                if (GUILayout.Button("Default", style))
                {
                    var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                    myColumnHeader.mode = MyMultiColumnHeader.Mode.DefaultHeader;
                }
                if (GUILayout.Button("No sort", style))
                {
                    var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                    myColumnHeader.mode = MyMultiColumnHeader.Mode.MinimumHeaderWithoutSorting;
                }

                GUILayout.Space(10);

                if (GUILayout.Button("values <-> controls", style))
                {
                    treeView.showControls = !treeView.showControls;
                }

                if (GUILayout.Button("Execute", style))
                {
                    processTreeItem(_TreeElements[0]);
                }
            }

            GUILayout.EndArea();
        }

        void processTreeItem(MyTreeElement element)
        {
            //// do
            //if (element.changeType == ChangeType.Modified)
            //{
            //    if (element.targetType == TargetType.State)
            //    {
            //        Debug.Log($"REMOVE state {element.destinationObject.GetType().Name} at '{element.path}'");
            //        var oldState = element.destinationObject as IBaseState;
            //        var oldNode = oldState.node;
            //        HFSMUtils.removeState(oldState, oldState.node);

            //        Debug.Log($"ADD state {element.sourceObject.GetType().Name} at '{element.path}'");
            //        var srcState = element.sourceObject as IBaseState;
            //        var newState = HFSMUtils.cloneTask(srcState, oldNode, dstLayer);
            //    }
            //    else
            //    {
            //        Debug.Log($"SET value at '{element.path}'");
            //    }
            //}
            //else if (element.changeType == ChangeType.Added)
            //{
            //    if (element.targetType == TargetType.Node)
            //    {
            //        Debug.Log($"ADD node at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.State)
            //    {
            //        Debug.Log($"ADD state at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Transition)
            //    {
            //        Debug.Log($"ADD transition at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Condition)
            //    {
            //        Debug.Log($"ADD condition at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Value)
            //    {
            //        Debug.Log($"ADD value at '{element.path}'");
            //    }
            //    else
            //    {
            //        Debug.Log($"SET field at '{element.path}'");
            //    }
            //}
            //else if (element.changeType == ChangeType.Removed)
            //{
            //    if (element.targetType == TargetType.Node)
            //    {
            //        Debug.Log($"REMOVE node at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.State)
            //    {
            //        Debug.Log($"REMOVE state at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Transition)
            //    {
            //        Debug.Log($"REMOVE transition at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Condition)
            //    {
            //        Debug.Log($"REMOVE condition at '{element.path}'");
            //    }
            //    else if (element.targetType == TargetType.Value)
            //    {
            //        Debug.Log($"REMOVE value at '{element.path}'");
            //    }
            //    else
            //    {
            //        Debug.Log($"REMOVE field at '{element.path}'");
            //    }
            //}

            //if (element.children != null)
            //{
            //    foreach (var child in element.children)
            //    {
            //        processTreeItem((MyTreeElement)child);
            //    }
            //}
        }

        ScriptableObject DeepClone(ScriptableObject src, UnityEngine.Object asset)
        {
            var cop = Instantiate(src);
            AssetDatabase.AddObjectToAsset(src, asset);

            var fields = TryGetFields(cop);
            foreach (var field in fields)
            {
                var obj = field.GetValue(cop);
                if (obj is ScriptableObject)
                {
                    var next = DeepClone(obj as ScriptableObject, asset);
                    field.SetValue(cop, next);
                }
            }

            return cop;
        }
    }

    internal class MyMultiColumnHeader : MultiColumnHeader
    {
        Mode m_Mode;

        public enum Mode
        {
            LargeHeader,
            DefaultHeader,
            MinimumHeaderWithoutSorting
        }

        public MyMultiColumnHeader(MultiColumnHeaderState state)
            : base(state)
        {
            mode = Mode.DefaultHeader;
        }

        public Mode mode
        {
            get
            {
                return m_Mode;
            }
            set
            {
                m_Mode = value;
                switch (m_Mode)
                {
                    case Mode.LargeHeader:
                        canSort = true;
                        height = 37f;
                        break;
                    case Mode.DefaultHeader:
                        canSort = true;
                        height = DefaultGUI.defaultHeight;
                        break;
                    case Mode.MinimumHeaderWithoutSorting:
                        canSort = false;
                        height = DefaultGUI.minimumHeight;
                        break;
                }
            }
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            // Default column header gui
            base.ColumnHeaderGUI(column, headerRect, columnIndex);

            // Add additional info for large header
            if (mode == Mode.LargeHeader)
            {
                // Show example overlay stuff on some of the columns
                if (columnIndex > 2)
                {
                    headerRect.xMax -= 3f;
                    var oldAlignment = EditorStyles.largeLabel.alignment;
                    EditorStyles.largeLabel.alignment = TextAnchor.UpperRight;
                    GUI.Label(headerRect, 36 + columnIndex + "%", EditorStyles.largeLabel);
                    EditorStyles.largeLabel.alignment = oldAlignment;
                }
            }
        }
    }
}
