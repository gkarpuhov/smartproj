using System;
using System.Collections.Generic;
using System.Text;

namespace Smartproj.Utils
{
    public static class TreeExt
    {
        public static int GetIndex(this ITree _node)
        {
            if (_node.Parent != null)
            {
                for (int i = 0; i < _node.Parent.Degree; i++)
                {
                    if (_node.Parent.GetChild(i).Equals(_node)) return i;
                }
            }
            return -1;
        }
        public static Stack<T> GetNodesStack<T>(this ITree _node) where T : ITree
        {
            return GetNodesStack<T>(_node, null);
        }
        public static Stack<T> GetNodesStack<T>(this ITree _node, Predicate<T> _filter) where T : ITree
        {
            Stack<T> stack = new Stack<T>();
            Queue<ITree> queue = new Queue<ITree>();
            if (_filter == null || _filter((T)_node))
            {
                stack.Push((T)_node);
            }

            queue.Enqueue(_node);

            while (queue.Count > 0)
            {
                int queueCount = queue.Count;
                for (int i = 0; i < queueCount; i++)
                {
                    ITree fromqueue = queue.Dequeue();
                    for (int j = 0; j < fromqueue.Degree; j++)
                    {
                        queue.Enqueue(fromqueue.GetChild(j));
                        if (_filter == null || _filter((T)fromqueue.GetChild(j)))
                        {
                            stack.Push((T)fromqueue.GetChild(j));
                        }
                    }
                }
            }

            return stack;
        }
        public static IList<T> ToList<T>(this T _first) where T : ITree
        {
            List<T> list = new List<T>();
            var stack = new Stack<ITree>();
            stack.Push(_first);

            while (stack.Count > 0)
            {
                ITree tree = stack.Pop();
                list.Add((T)tree);
                for (var i = 0; i < tree.Degree; i++)
                {
                    stack.Push(tree.GetChild(i));
                }
            }

            return list;
        }
        public static IEnumerator<T> GetTreeEnumerator<T>(this ITree _first) where T : ITree
        {
            var stack = new Stack<ITree>();
            stack.Push(_first);

            while (stack.Count > 0)
            {
                var tree = stack.Pop();
                if (typeof(T).IsInstanceOfType(tree))
                {
                    yield return (T)tree;
                }
                for (var i = 0; i < tree.Degree; i++)
                {
                    stack.Push(tree.GetChild(i));
                }
            }
        }
        public static IEnumerator<T> GetTreeEnumerator<T>(this ITree _first, Predicate<ITree> _filter) where T : ITree
        {
            var stack = new Stack<ITree>();
            stack.Push(_first);

            while (stack.Count > 0)
            {
                var tree = stack.Pop();
                if (_filter == null || _filter(tree))
                {
                    yield return (T)tree;
                }
                for (var i = 0; i < tree.Degree; i++)
                {
                    stack.Push(tree.GetChild(i));
                }
            }
        }
        public static IEnumerable<T> GetTreeItems<T>(this ITree _first, Predicate<ITree> _filter) where T : ITree
        {
            return new AnyEnumerable<T>(_first.GetTreeEnumerator<T>(_filter));
        }
        public static IEnumerable<T> GetTreeItems<T>(this ITree _first) where T : ITree
        {
            return new AnyEnumerable<T>(_first.GetTreeEnumerator<T>());
        }
        public static bool IsAncestorOf(this ITree _tree, ITree _node)
        {
            if (!_tree.Equals(_node) && _tree.Root.Equals(_node.Root))
            {
                ITree current = _node;
                while (current != null)
                {
                    if (_tree.Equals(current)) return true;
                    current = current.Parent;
                }
            }

            return false;
        }
        public static IList<T> Ancestors<T>(this T _tree) where T : ITree
        {
            List<T> ancestors = new List<T>();
            T current = (T)_tree.Parent;
            while (current != null)
            {
                ancestors.Add(current);
                current = (T)current.Parent;
            }
            return ancestors;
        }
        public static void ApplyForAll(this ITree _node, Action<ITree> _action)
        {
            _action(_node);
            for (int i = 0; i < _node.Degree; i++)
            {
                _node.GetChild(i).ApplyForAll(_action);
            }
        }
        public static bool ContainsInBranch(this ITree _branchnode, ITree _node)
        {
            if (!_branchnode.Root.Equals(_node.Root)) return false;

            byte direction = 0;
            ITree current = _branchnode;
            Stack<ITree> stackDown = new Stack<ITree>();

            while (current != null)
            {
                if (current.Equals(_node)) return true;

                if (direction == 0)
                {
                    current = current.Parent;

                    if (current == null)
                    {
                        direction = 1;
                        for (int i = 0; i < _branchnode.Degree; i++)
                        {
                            stackDown.Push(_branchnode.GetChild(i));
                        }
                    }
                }

                if (direction == 1)
                {
                    if (stackDown.Count > 0)
                    {
                        current = stackDown.Pop();
                        for (int i = 0; i < current.Degree; i++)
                        {
                            stackDown.Push(current.GetChild(i));
                        }
                    }
                    else
                        current = null;
                }
            }

            return false;
        }
        public static bool Contains<T>(this ITree _node, Predicate<T> _predicate) where T : ITree
        {
            Stack<ITree> stackDown = new Stack<ITree>();
            stackDown.Push(_node);

            while (stackDown.Count > 0)
            {
                ITree current = stackDown.Pop();

                if (typeof(T).IsInstanceOfType(current) && _predicate((T)current)) return true;

                for (int i = 0; i < current.Degree; i++)
                {
                    stackDown.Push(current.GetChild(i));
                }
            }

            return false;
        }
        public static T FindInBranch<T>(this ITree _branchnode, Predicate<T> _predicate) where T : ITree
        {
            byte direction = 0;
            ITree current = _branchnode;
            Stack<ITree> stackDown = new Stack<ITree>();

            while (current != null)
            {
                if (typeof(T).IsInstanceOfType(current) && _predicate((T)current)) return (T)current;

                if (direction == 0)
                {
                    current = current.Parent;

                    if (current == null)
                    {
                        direction = 1;
                        for (int i = 0; i < _branchnode.Degree; i++)
                        {
                            stackDown.Push(_branchnode.GetChild(i));
                        }
                    }
                }

                if (direction == 1)
                {
                    if (stackDown.Count > 0)
                    {
                        current = stackDown.Pop();
                        for (int i = 0; i < current.Degree; i++)
                        {
                            stackDown.Push(current.GetChild(i));
                        }
                    }
                    else
                        current = null;
                }
            }

            return default;
        }
        public static T FindAny<T>(this T _first, T _object, bool _recursive, out int _index) where T : ITree
        {
            return _first.FindAny<T, T>(_object, _recursive, (x, y) => { return x.Equals(y); }, out _index);
        }
        public static TX FindAny<TX, TY>(this TX _first, TY _object, bool _recursive, Compliance<TX, TY> _compliance, out int _index) where TX : ITree
        {
            for (int i = 0; i < _first.Degree; i++)
            {
                if (_compliance((TX)_first.GetChild(i), _object))
                {
                    _index = i;
                    return _first;
                }
            }

            ITree result = null;

            if (_recursive)
            {
                for (int i = 0; i < _first.Degree; i++)
                {
                    if ((result = FindAny((TX)_first.GetChild(i), _object, true, _compliance, out _index)) != null)
                    {
                        return (TX)result;
                    }
                }
            }

            _index = -1;
            return (TX)result;
        }
        public static IList<TOutput> GetPath<TOutput>(this ITree _first, Converter<ITree, TOutput> _converter, bool _includeThis)
        {
            var path = new List<TOutput>();

            if (_includeThis)
            {
                path.Add(_converter(_first));
            }

            for (var node = _first.Parent; node != null; node = node.Parent)
            {
                path.Add(_converter(node));
            }

            path.Reverse();

            return path;
        }
        public static string GetPath<TOutput>(this ITree _first, Converter<ITree, TOutput> _converter, bool _includeThis, string _separator)
        {
            StringBuilder sb = new StringBuilder();

            if (_includeThis)
            {
                sb.Append(_converter(_first));
            }

            for (var node = _first.Parent; node != null; node = node.Parent)
            {
                if (sb.Length > 0)
                {
                    sb.Insert(0, _separator);
                }

                sb.Insert(0, _converter(node));
            }

            return sb.ToString();
        }
        public static IList<TOutput> GetPath<TOutput>(this ITree _first, Converter<ITree, TOutput> _converter)
        {
            if (_converter == null)
            {
                throw new ArgumentNullException("converter");
            }

            return _first.GetPath(_converter, false);
        }
    }

}
