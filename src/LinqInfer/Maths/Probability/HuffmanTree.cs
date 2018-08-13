using System.Collections.Generic;
using System.Linq;
using System;

namespace LinqInfer.Maths.Probability
{
    class HuffmanTree<T> where T : IEquatable<T>, IComparable<T>
    {
        readonly Node _root;
        readonly IDictionary<T, Node> _lookup;

        public HuffmanTree(IEnumerable<T> corpus) : this(corpus.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count()))
        {
        }

        public HuffmanTree(IDictionary<T, int> itemFrequencies)
        {
            _root = Process(itemFrequencies);
            _lookup = RecurseTreeAndAssignCode(_root, itemFrequencies.Count).ToDictionary(n => n.Value);
        }

        public Node Root { get { return _root; } }

        public IDictionary<T, Node> Lookup { get { return _lookup; } }

        static Node Process(IDictionary<T, int> corpus)
        {
            var queue = new Queue<Node>(corpus.OrderBy(w => w.Value).Select(w => new Node()
            {
                Frequency = w.Value,
                Value = w.Key
            }));

            while (queue.Count > 1)
            {
                var x1 = queue.Dequeue();
                var x2 = queue.Dequeue();

                queue.Enqueue(new Node(x1, x2));

                queue = new Queue<Node>(queue.OrderBy(x => x.Frequency)); // Not particularly efficient
            }

            var root = queue.Dequeue();

            return root;
        }

        static IList<Node> RecurseTreeAndAssignCode(Node root, int size = 1024)
        {
            long x = 0;

            Node cur;

            var items = new List<Node>(size);

            var nodes = new Stack<Node>(size);

            nodes.Push(root);

            while (nodes.Count > 0)
            {
                cur = nodes.Pop();

                if (cur.IsLeaf)
                {
                    items.Add(cur);

                    continue;
                }
                else
                {
                    x = 1;

                    foreach (var node in cur.Children)
                    {
                        node.Code = (node.Parent.Code << 1) + x++;

                        nodes.Push(node);
                    }
                }
            }

            return items;
        }

        public class Node : Item
        {
            public Node()
            {
                Children = Enumerable.Empty<Node>();
            }

            public Node(Node word1, Node word2)
            {
                var children = new List<Node>();

                children.Add(word1);
                children.Add(word2);

                word1.Parent = this;
                word2.Parent = this;

                Frequency = word1.Frequency + word2.Frequency;
                Children = children;
            }

            public Node Parent { get; private set; }

            public bool IsLeaf
            {
                get
                {
                    return !Children.Any();
                }
            }

            public IEnumerable<Node> Children { get; private set; }
        }

        public class Item
        {
            public int Frequency { get; set; }
            public T Value { get; set; }
            public long Code { get; set; }
            public string BinaryCode
            {
                get
                {
                    return Convert.ToString(Code, 2);
                }
            }
        }
    }
}
