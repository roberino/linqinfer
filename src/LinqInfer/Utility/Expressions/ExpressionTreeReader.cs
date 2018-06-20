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

            foreach (var c in input)
            {
                var type = GetType(state.Type, c);

                if (type == TokenType.Space) continue;

                if (type == state.Type && type != TokenType.GroupOpen)
                {
                    state.Value += c;
                    continue;
                }

                if (type == TokenType.GroupClose || type == TokenType.Separator)
                {
                    state = state.Parent;
                    continue;
                }

                if (type == TokenType.Operator)
                {
                    root = new ExpressionTree() { Type = type, Value = c.ToString() };
                    root.AddChild(state.LocalRoot);
                    state = root;
                    continue;
                }

                state = state.AddChild(type, c.ToString());
            }

            return root;
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