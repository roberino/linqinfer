using System;
using System.Collections.Generic;
using System.Linq;

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

        public ExpressionTree ParentOrSelf => Parent ?? this;

        public ExpressionTree LocalRoot =>
            MoveToAncestorOrSelf(e => e.Type == TokenType.GroupOpen
                                || e.Type == TokenType.Operator || e.Type == TokenType.Condition);

        public bool IsFull => Type.Capacity() == _children.Count;

        public ExpressionTree MoveToEmptyAncestorOrSelf()
        {
            var next = this;

            while (next.IsFull)
            {
                next = next.Parent;
            }

            return next;
        }

        public ExpressionTree MoveToParentOrRoot() => Type == TokenType.Root ? this : Parent;

        public ExpressionTree MoveToAncestorOrSelf(Func<ExpressionTree, bool> predicate)
        {
            if (Parent == null) return this;

            var parent = Parent;

            while (parent?.Parent != null && !predicate(parent))
            {
                parent = parent.Parent;
            }

            return parent;
        }

        ExpressionTree TakeLastChild()
        {
            var arg = _children.Last();
            _children.Remove(arg);
            return arg;
        }

        ExpressionTree DetatchFromParent()
        {
            var parent = Parent;
            parent?._children.Remove(this);
            return parent;
        }

        public ExpressionTree InsertCondition()
        {
            var localRoot = MoveToAncestorOrSelf(e =>
            (e.Type == TokenType.Operator && e.Value.IsBooleanOperator())
            || e.Parent?.Type == TokenType.Root);

            if (localRoot.Type != TokenType.Root) localRoot = localRoot.Parent;

            var newNode = new ExpressionTree() { Type = TokenType.Condition, Value = "?" };

            newNode.AddChild(localRoot.TakeLastChild());
            newNode.Parent = localRoot;

            localRoot.AddChild(newNode);

            return newNode;
        }

        public ExpressionTree InsertOperator(string value)
        {
            if (Type == TokenType.Operator || Type == TokenType.Condition)
            {
                return AddChild(TokenType.Operator, value);
            }
            
            var localRoot = LocalRoot;

            var newNode = new ExpressionTree() { Type = TokenType.Operator, Value = value };

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
                newNode.AddChild(localRoot.TakeLastChild());

                localRoot.AddChild(newNode);
            }

            return newNode;
        }

        public ExpressionTree AddChild(TokenType type, string value)
        {
            var child = new ExpressionTree() { Type = type, Value = value, Parent = this };

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
    }
}