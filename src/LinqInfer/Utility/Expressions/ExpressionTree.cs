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
                                || e.Type == TokenType.Operator);

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

        ExpressionTree TakeLastArg()
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
        
        public ExpressionTree InsertOperator(string value)
        {
            var localRoot = LocalRoot;
                
            var newNode = new ExpressionTree() {Type = TokenType.Operator, Value = value};
            
            if (localRoot.Type == TokenType.Operator)
            {
                if (OperatorPrecedence.TakesPrecedence(value, localRoot.Value))
                {
                    newNode.AddChild(localRoot.TakeLastArg());
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
                newNode.Parent = localRoot;
                
                var newChild = localRoot.Children.Single();

                newNode.AddChild(newChild);
                newChild.Parent = newNode;

                localRoot._children.Clear();
                localRoot.AddChild(newNode);
            }

            return newNode;
        }

        public ExpressionTree AddChild(TokenType type, string value)
        {
            var child = new ExpressionTree() {Type = type, Value = value, Parent = this};

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

    internal enum TokenType
    {
        Unknown,
        Operator,
        Name,
        Navigate,
        Space,
        GroupClose,
        GroupOpen,
        Literal,
        Separator
    }
}