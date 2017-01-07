using System.Collections.Generic;

namespace LinqInfer.Text
{
    /// <summary>
    /// Represents a document which is an enumeration of text tokens
    /// </summary>
    public class TokenisedTextDocument
    {
        public TokenisedTextDocument(string id, IEnumerable<IToken> tokens)
        {
            Id = id;
            Tokens = tokens;
        }

        /// <summary>
        /// The document unique identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The sequence of tokens within the document
        /// </summary>
        public IEnumerable<IToken> Tokens { get; private set; }
    }
}