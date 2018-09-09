using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility.Expressions
{
    public sealed class Parameter
    {
        internal Parameter(string name, int index, Type type = null)
        {
            Name = name;
            Index = index;
            Type = type ?? typeof(object);
            IsTypeKnown = type != null;
        }

        public string Name { get; }
        public Type Type { get; }
        public int Index { get; }
        public bool IsTypeKnown { get; set; }

        internal static IEnumerable<Parameter> GetParameters(ExpressionTree expressionTree)
        {
            var i = 0;

            if (expressionTree.Type == TokenType.Name)
            {
                yield return new Parameter(expressionTree.Value, i, expressionTree.Children.SingleOrDefault()?.AsType());
                yield break;
            }

            if (expressionTree.Children.Count == 1 && expressionTree.Children.Single().Type == TokenType.Name)
            {
                yield return new Parameter(expressionTree.Children.Single().Value, i);
                yield break;
            }

            foreach (var p in expressionTree.Parameters)
            {
                if (p.Children.Any())
                {
                    yield return new Parameter(p.Value, i, p.Children.Single().AsType());
                }
                else
                {
                    yield return new Parameter(p.Value, i++);
                }

                i++;
            }
        }
    }
}