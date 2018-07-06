using System.Linq;

namespace LinqInfer.Utility.Expressions
{
    internal class ExpressionTreeReader
    {
        // (x + 1) * (y - 2)

        // = 

        //  *
        //     => +
        //         => x
        //         => 1
        //      => -
        //         => y
        //         => 2

        public ExpressionTree Read(string input)
        {
            var state = new ExpressionTree();
            var root = state;
            var type = TokenType.Unknown;
            var pos = 0;

            foreach (var c in input)
            {
                var lastType = type;

                type = state.Type.GetTokenType(c);

                if (type == TokenType.Space) continue;
                
                if (type == TokenType.GroupClose)
                {
                    state = state.MoveToAncestorOrSelf(e => e.Type == TokenType.GroupOpen);
                    continue;
                }

                if (type == lastType && type.ShouldAccumulate())
                {
                    state.Value += c;
                    continue;
                }

                if (type == TokenType.Separator)
                {
                    state = state.LocalRoot;
                    continue;
                }

                if (type == TokenType.Operator)
                {
                    state = state.InsertOperator(c.ToString());
                    state.Position = pos++;
                    continue;
                }

                state = state.AddChild(type, c.ToString());
                state.Position = pos++;
            }

            return root.Children.Single();
        }
    }
}