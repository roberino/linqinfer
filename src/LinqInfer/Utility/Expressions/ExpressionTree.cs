using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    internal class ExpressionTree
    {
        IList<ExpressionTree> _children = new List<ExpressionTree>();

        public ExpressionTree Parent { get; set; }
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }

        public IEnumerable<ExpressionTree> Children => _children;

        public bool IsOperation
        {
            get
            {
                if (Type != TokenType.Operator) return false;

                if (Value.AsExpressionType() != ExpressionType.Subtract) return true;

                return IsFull;
            }
        }

        public ExpressionTree LocalRoot => MoveToAncestorOrRoot(e =>
            e.Type == TokenType.GroupOpen ||
            e.IsOperation ||
            e.Type == TokenType.Condition, false, true);

        public bool IsFull => Type.Capacity() == _children.Count;

        public bool IsEmpty => _children.Count == 0;

        public ExpressionTree MoveToEmptyAncestorOrSelf()
        {
            var next = this;

            while (next.IsFull)
            {
                next = next.Parent;
            }

            return next;
        }

        public ExpressionTree MoveToAncestorOrRoot(Func<ExpressionTree, bool> predicate, bool greedy = false, bool includeSelf = false)
        {
            if (Type == TokenType.Root || (includeSelf && predicate(this))) return this;

            var parent = Parent;

            ExpressionTree candidate = null;

            while (parent?.Parent != null)
            {
                if (predicate(parent))
                {
                    candidate = parent;

                    if (!greedy)
                    {
                        return candidate;
                    }
                }
                else
                {
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }

                parent = parent.Parent;
            }

            return parent;
        }

        public ExpressionTree InsertCondition(int position)
        {
            var localRoot = MoveToAncestorOrRoot(e =>
            (e.Type == TokenType.Operator && e.Value.IsBooleanOperator())
            || e.Parent?.Type == TokenType.Root, true);

            if (localRoot.Type != TokenType.Root) localRoot = localRoot.Parent;

            var newNode = new ExpressionTree() { Type = TokenType.Condition, Value = "?", Position = position };

            newNode.AddChild(localRoot.TakeLastChild());
            newNode.Parent = localRoot;

            localRoot.AddChild(newNode);

            return newNode;
        }

        public ExpressionTree InsertOperator(string value, int position)
        {
            if (Type == TokenType.Operator || Type == TokenType.Condition)
            {
                return AddChild(TokenType.Operator, value, position);
            }

            var localRoot = LocalRoot;

            var newNode = new ExpressionTree() { Type = TokenType.Operator, Value = value, Position = position };

            if (localRoot.Type == TokenType.Operator)
            {
                if (OperatorPrecedence.TakesPrecedence(value, localRoot.Value))
                {
                    newNode.AddChild(localRoot.TakeLastChild());
                    newNode.Parent = localRoot;
                    localRoot.AddChild(newNode);
                }
                else
                {
                    newNode.Parent = localRoot.DetatchFromParent();
                    newNode.Parent?.AddChild(newNode);
                    newNode.AddChild(localRoot);
                }
            }
            else
            {
                if (!localRoot.IsEmpty)
                {
                    newNode.AddChild(localRoot.TakeLastChild());
                }

                localRoot.AddChild(newNode);
            }

            return newNode;
        }

        public ExpressionTree AddChild(TokenType type, string value, int position)
        {
            var child = new ExpressionTree() { Type = type, Value = value, Parent = this, Position = position };

            _children.Add(child);

            return child;
        }

        public ExpressionTree AddChild(ExpressionTree expressionTree)
        {
            var child = expressionTree;

            _children.Add(child);

            child.Parent = this;

            return child;
        }

        public override string ToString() => $"{Type}:{Value}";

        private ExpressionTree TakeLastChild()
        {
            var arg = _children.Last();
            _children.Remove(arg);
            return arg;
        }

        private ExpressionTree DetatchFromParent()
        {
            var parent = Parent;
            parent?._children.Remove(this);
            return parent;
        }
    }
}