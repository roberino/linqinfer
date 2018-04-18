using System;
using System.Linq;

namespace LinqInfer.Text
{
    public static class TokenExtensions
    {
        public static string NormalForm(this IToken token)
        {
            return token.Text.ToLowerInvariant();
        }
    }

    internal class Token : IToken
    {
        public Token(string value, int index, TokenType? type = null)
        {
            Text = value;
            Index = index;

            if (type.HasValue) Type = type.Value;
        }
        public Token(Token previous, string value, int index)
        {
            Text = value;
            Index = index;
            Type = GetTokenType(previous, value);
        }

        public int Index { get; set; }
        public string Text { get; set; }
        public TokenType Type { get; set; }
        public byte Weight { get; set; }
        public bool IsCapitalised { get { return Type == TokenType.Word && char.IsUpper(Text[0]); } }

        public TokenType GetTokenType(Token previous, string token)
        {
            if (string.IsNullOrEmpty(token)) return TokenType.Null;

            if (char.IsNumber(token[0]) && token.All(c => char.IsDigit(c)))
            {
                return TokenType.Number;
            }
            if ((previous.Type == TokenType.Word || previous.Type == TokenType.Number) && token == ".")
            {
                return TokenType.SentenceEnd;
            }

            if (token.All(c => char.IsWhiteSpace(c))) return TokenType.Space;

            if (token.Any(c => !char.IsLetterOrDigit(c))) return TokenType.Symbol;

            return TokenType.Word;
        }

        public override string ToString()
        {
            return string.Format("{0}:\t[{1}]\t\t{2}", Index, Type, Text);
        }

        public bool Equals(IToken other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (Index != other.Index || Weight != other.Weight || Type != other.Type) return false;

            return (string.Equals(Text, other.Text));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IToken);
        }

        public override int GetHashCode()
        {
            return new Tuple<TokenType, int, int, string>(Type, Index, Weight, Text).GetHashCode();
        }
    }
}