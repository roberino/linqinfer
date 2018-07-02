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

                type = GetType(state.Type, c);

                if (type == TokenType.Space) continue;

                if (type == lastType && type != TokenType.GroupOpen)
                {
                    state.Value += c;
                    continue;
                }

                if (type == TokenType.GroupClose)
                {
                    state = state.MoveToAncestorOrSelf(e => e.Type == TokenType.GroupOpen);
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

        static TokenType GetType(TokenType currentTokenType, char c)
        {
            switch (c)
            {
                case '(': return TokenType.GroupOpen;
                case ')': return TokenType.GroupClose;
                case ' ': return TokenType.Space;
                case '+':
                case '-':
                case '/':
                case '*':
                case '=':
                case '!':
                case '|':
                case '&':
                    return TokenType.Operator;
                case ',':
                    return TokenType.Separator;
                case '.':
                    if (currentTokenType == TokenType.Literal) return TokenType.Literal;
                    if (currentTokenType == TokenType.Name) return TokenType.Navigate;
                    return TokenType.Unknown;
                default:
                    if (char.IsDigit(c))
                    {
                        if (currentTokenType == TokenType.Name) return TokenType.Name;
                        return TokenType.Literal;
                    }

                    if (char.IsLetter(c)) return TokenType.Name;
                    return TokenType.Unknown;

            }
        }
    }
}