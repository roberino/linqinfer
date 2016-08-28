using System.Collections.Generic;

namespace LinqInfer.Text
{
    public class TokenisedTextDocument
    {
        public TokenisedTextDocument(string id, IEnumerable<IToken> tokens)
        {
            Id = id;
            Tokens = tokens;
        }

        public string Id { get; private set; }
        public IEnumerable<IToken> Tokens { get; private set; }
    }
}