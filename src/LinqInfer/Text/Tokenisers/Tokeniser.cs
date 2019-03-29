using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Text.Tokenisers
{
    public class Tokeniser : ITokeniser
    {
        public IEnumerable<IToken> Tokenise(string corpus, int? indexOffset = null)
        {
            if (corpus == null) yield break;

            var currentToken = new StringBuilder(16);

            var lastToken = new Token(null, -1);

            var type = TokenType.Null;

            var i = 0;
            var index = indexOffset.GetValueOrDefault();

            while (i < corpus.Length)
            {
                var c = corpus[i];

                if (char.IsLetterOrDigit(c))
                {
                    if (currentToken.Length > 0 && type == TokenType.Null)
                    {
                        lastToken = YieldToken(currentToken, lastToken, type, index);

                        yield return lastToken;
                    }

                    currentToken.Append(c);

                    if (type == TokenType.Null && char.IsDigit(c))
                    {
                        type = TokenType.Number;
                    }
                    else
                    {
                        if (char.IsLetter(c))
                        {
                            type = TokenType.Word;
                        }
                    }
                }
                else
                {
                    if (c == '.' && type == TokenType.Number)
                    {
                        currentToken.Append(c);
                    }
                    else
                    {
                        if (currentToken.Length > 0)
                        {
                            lastToken = YieldToken(currentToken, lastToken, type, index);

                            yield return lastToken;
                        }

                        if (!(lastToken.IsWhiteSpace() && char.IsWhiteSpace(c)))
                        {
                            type = TokenType.Null;
                            currentToken.Append(c);
                        }
                    }
                }

                index++;
                i++;
            }

            if (currentToken.Length > 0)
            {
                yield return YieldToken(currentToken, lastToken, type, index);
            }
        }

        static Token YieldToken(StringBuilder currentToken, Token lastToken, TokenType type, int i)
        {
            Token nextToken = null;

            if (currentToken.Length > 0)
            {
                if (type == TokenType.Null)
                {
                    nextToken = new Token(lastToken, currentToken.ToString(), i - currentToken.Length);
                }
                else
                {
                    nextToken = new Token(currentToken.ToString(), i - currentToken.Length, type);
                }

                currentToken.Clear();
            }

            return nextToken;
        }
    }
}
