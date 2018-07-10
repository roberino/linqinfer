using System.Linq;

namespace LinqInfer.Utility.Expressions
{
    internal class ExpressionTreeReader
    {
        private static (string token, int position) GreedyRead(char currentChar, string input, int pos, TokenType context)
        {
            string result = currentChar.ToString();
            int posNew = pos + 1;
            var lastType = context;

            if (posNew < input.Length)
            {
                for (var i = posNew; i < input.Length; i++)
                {
                    var nextChar = input[posNew];
                    var nextType = context.GetTokenType(nextChar);

                    if (nextType != lastType || !nextType.ShouldAccumulate())
                    {
                        break;
                    }

                    result += nextChar;

                    posNew = i;
                }
            }

            return (result, posNew - 1);
        }

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
            var state = new ExpressionTree() {Type = TokenType.Root};
            var root = state;
            var type = state.Type;

            for (var pos = 0; pos < input.Length; pos++)
            {
                var c = input[pos];
                var lastType = type;
                (string token, int position) tokenInf = (c.ToString(), pos);

                type = state.Type.GetTokenType(c);

                if (type == TokenType.Space) continue;

                if (type == TokenType.Split)
                {
                    state = state.MoveToAncestorOrSelf(e => e.Type == TokenType.Condition);
                    continue;
                }

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
                    state.Position = pos;
                    continue;
                }

                if (type == TokenType.Condition)
                {
                    state = state.InsertCondition();
                    state.Position = pos;
                    continue;
                }

                state = state.MoveToEmptyAncestorOrSelf();

                state = state.AddChild(type, c.ToString());
                state.Position = pos;
            }

            return root.Children.Single();
        }
    }
}