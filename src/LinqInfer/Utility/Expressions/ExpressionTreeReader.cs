using System.Linq;

namespace LinqInfer.Utility.Expressions
{
    internal class ExpressionTreeReader
    {
        private static (string token, int position) GreedyRead(char currentChar, string input, int pos, TokenType context)
        {
            var result = currentChar.ToString();
            var lastType = context;

            if (pos >= input.Length) return (result, pos);

            int i;

            for (i = pos + 1; i < input.Length; i++)
            {
                var nextChar = input[i];
                var nextType = context.GetTokenType(nextChar);

                if (nextType != lastType || !nextType.ShouldAccumulate())
                {
                    return (result, i - 1);
                }

                result += nextChar;
            }

            return (result, i);
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
                    state = state.MoveToAncestorOrRoot(e => e.Type == TokenType.Condition);
                    continue;
                }

                if (type == TokenType.GroupClose)
                {
                    state = state.MoveToAncestorOrRoot(e => e.Type == TokenType.GroupOpen);
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
                    var readState = GreedyRead(c, input, pos, type);

                    state = state.InsertOperator(readState.token);
                    state.Position = pos;
                    pos = readState.position;
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