using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class ExpressionTree
    {
        readonly List<ExpressionTree> _children = new List<ExpressionTree>();

        ExpressionTree()
        {
        }

        public static ExpressionTree Root => new ExpressionTree() {Type = TokenType.Root};

        public ExpressionTree Parent { get;  private set; }
        public TokenType Type { get; private set; }
        public string Value { get; private set; }
        public int Position { get; private set; }
        public int SegmentIndex { get; set; }

        public int Depth
        {
            get
            {
                var depth = 0;

                var next = this;

                while (next.Parent != null)
                {
                    if (next.Type == TokenType.GroupOpen) depth++;

                    next = next.Parent;
                }

                return depth;
            }
        }

        public IReadOnlyCollection<ExpressionTree> Children => _children;

        public bool IsOperation
        {
            get
            {
                if (Type != TokenType.Operator) return false;

                return Value.AsExpressionType() != ExpressionType.Subtract || IsFull;
            }
        }

        public bool IsFunction
        {
            get
            {
                if (Type != TokenType.Name) return false;

                return _children.Count == 1 && _children[0].Type == TokenType.GroupOpen;
            }
        }

        public bool IsBoolean
        {
            get
            {
                if (IsOperation && Value.IsBooleanOperator()) return true;

                return Type == TokenType.GroupOpen && _children.Count == 1 && _children[0].IsBoolean;
            }
        }

        public ExpressionTree LocalRoot
        {
            get
            {
                var root = MoveToAncestorOrRoot(e =>
                    (e.Type == TokenType.GroupOpen && !e.Parent.IsFunction) ||
                    e.Type == TokenType.Separator ||
                    e.IsOperation ||
                    e.Type == TokenType.Condition, false, true);

                return root;
            }
        }

        public bool IsFull => Type.Capacity() == _children.Count;

        public bool IsEmpty => _children.Count == 0;

        public ExpressionTree MoveToEmptyAncestorOrSelf()
        {
            var next = this;

            while (next.IsFull && !next.IsOperation)
            {
                next = next.Parent;
            }

            return next;
        }

        public ExpressionTree MoveToGroup() => MoveToAncestorOrRoot(e => e.Type == TokenType.GroupOpen, false, true);

        public ExpressionTree MoveToAncestorOrRoot(Func<ExpressionTree, bool> predicate, bool greedy = false, bool includeSelf = false)
        {
            if (Type == TokenType.Root) return this;

            var parent = includeSelf ? this : Parent;

            ExpressionTree candidate = null;

            while (parent?.Parent != null)
            {
                if (predicate(parent))
                {
                    candidate = parent;

                    if (!greedy || candidate.Type == TokenType.GroupOpen)
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

        public ExpressionTree InsertSeparator(int position)
        {
            var state = this;

            if (Type == TokenType.Separator)
            {
                state = MoveToGroup();
            }
            else
            {
                var sep = new ExpressionTree()
                {
                    Type = TokenType.Separator,
                    Position = position
                };

                sep.AddChild(TakeLastChild());

                AddChild(sep);
            }

            state.SegmentIndex++;

            state = state.AddChild(new ExpressionTree()
            {
                Type = TokenType.Separator,
                Position = position
            });

            return state;
        }

        public ExpressionTree InsertCondition(int position)
        {
            var localRoot = MoveToAncestorOrRoot(e => e.IsBoolean || e.Parent?.Type == TokenType.Root, true, true);

            if (localRoot.Type != TokenType.Root && localRoot.Type != TokenType.GroupOpen) localRoot = localRoot.Parent;

            var newNode = new ExpressionTree() { Type = TokenType.Condition, Value = ExpressionType.Conditional.AsString(), Position = position };

            newNode.AddChild(localRoot.TakeLastChild());
            newNode.Parent = localRoot;

            localRoot.AddChild(newNode);

            return newNode;
        }

        public ExpressionTree InsertChildAsParent(ExpressionTree newParent)
        {
            if (!IsEmpty)
            {
                newParent.AddChild(TakeLastChild());
            }

            AddChild(newParent);
            return newParent;
        }

        public ExpressionTree SwapParent(ExpressionTree newParent)
        {
            DetatchFromParent()?.AddChild(newParent);
            newParent.AddChild(this);
            return newParent;
        }

        public ExpressionTree InsertOperator(string value, int position)
        {
            var newNode = new ExpressionTree()
            {
                Type = TokenType.Operator,
                Value = value,
                Position = position
            };

            if ((Type == TokenType.Operator || Type == TokenType.Condition) 
                && !IsFull)
            {
                return AddChild(newNode);
            }

            var localRoot = LocalRoot;

            if (localRoot.Type == TokenType.Operator)
            {
                if (OperatorPrecedence.TakesPrecedence(value, localRoot.Value))
                {
                    localRoot.InsertChildAsParent(newNode);
                }
                else
                {
                    localRoot.SwapParent(newNode);
                }
            }
            else
            {
                localRoot.InsertChildAsParent(newNode);
            }

            return newNode;
        }

        public ExpressionTree InsertChild(TokenType type, string value, int position)
        {
            //var context = MoveToEmptyAncestorOrSelf();

            var child = new ExpressionTree()
            {
                Type = type,
                Value = value,
                Parent = this,
                Position = position,
            };

            _children.Add(child);

            if (Type == TokenType.Operator
                && _children.Count == 1
                && Value.AsExpressionType() == ExpressionType.Subtract)
            {
                Type = TokenType.Negation;
            }
            
            return child.MoveToEmptyAncestorOrSelf();
        }

        public ExpressionTree AddChild(ExpressionTree expressionTree)
        {
            var child = expressionTree;

            _children.Add(child);

            child.Parent = this;

            return child;
        }

        public override string ToString() => $"{Type}:{Value}";

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
    }
}