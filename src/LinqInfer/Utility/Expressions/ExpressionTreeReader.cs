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
            var state = ExpressionTree.Root;
            var reader = new StringNavigator<TokenType>(input, (t, c) => t.GetTokenType(c), t => t.ShouldAccumulate());

            while (reader.ReadNextToken())
            {
                switch (reader.TokenClass)
                {
                    case TokenType.Space:
                        continue;
                    case TokenType.Split:
                        {
                            state = state.MoveToAncestorOrRoot(e => e.Type == TokenType.Condition);
                            continue;
                        }
                    case TokenType.GroupClose:
                        {
                            state = state.MoveToGroup().Parent;

                            continue;
                        }
                    case TokenType.Separator:
                        {
                            continue;
                        }
                    case TokenType.Operator:
                        {
                            state = state.InsertOperator(reader.CurrentToken, reader.StartPosition);
                            continue;
                        }
                    case TokenType.Condition:
                        {
                            state = state.InsertCondition(reader.StartPosition);
                            continue;
                        }
                    case TokenType.Unknown:
                    {
                        throw new CompileException(reader.CurrentToken, reader.StartPosition, CompileErrorReason.UnknownToken);
                    }
                }

                state = state.MoveToEmptyAncestorOrSelf();

                state = state.AddChild(reader.TokenClass, reader.CurrentToken, reader.StartPosition);
            }

            state = state.MoveToGroup();

            if (state.Depth != 0)
            {
                throw new CompileException(state.Value, state.Position, CompileErrorReason.EndOfStream);
            }

            return state;
        }
    }
}