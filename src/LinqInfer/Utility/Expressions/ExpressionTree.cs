using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Utility.Expressions
{
    internal class ExpressionTree
    {
        IList<ExpressionTree> _children = new List<ExpressionTree>();

        public ExpressionTree Parent { get; set; }
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public IEnumerable<ExpressionTree> Children => _children;

        public ExpressionTree LocalRoot
        {
            get
            {
                var parent = Parent;

                while (parent != null && parent.Type != TokenType.GroupOpen)
                {
                    parent = parent.Parent;
                }

                return parent;
            }
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