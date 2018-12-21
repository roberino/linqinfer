using LinqInfer.Text.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class TokeniserExtensions
    {
        /// <summary>
        /// Returns a corpus from a text reader
        /// </summary>
        public static ICorpus CreateCorpus(this TextReader textReader, ITokeniser tokeniser = null)
        {
            return new TextReaderToCorpusAdapter(tokeniser).CreateCorpus(textReader);
        }

        /// <summary>
        /// Converts an enumeration of XML documents
        /// into an enumeration of tokenised text documents.
        /// </summary>
        /// <param name="documents">The documents</param>
        /// <param name="keySelector">A function which returns a unique document key for a document</param>
        /// <param name="tokeniser">An optional tokeniser</param>
        /// <returns></returns>
        public static IEnumerable<TokenisedTextDocument> AsTokenisedDocuments(this IEnumerable<XDocument> documents, Func<XDocument, string> keySelector, ITokeniser tokeniser = null)
        {
            var index = new DocumentIndex(tokeniser);

            return index.Tokenise(documents, keySelector);
        }

        /// <summary>
        /// Converts a string into an enumeration of tokens.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="tokeniser">An optional tokeniser</param>
        /// <returns>An enumeration of <see cref="IToken"/></returns>
        public static IEnumerable<IToken> Tokenise(this string text, ITokeniser tokeniser = null)
        {
            return ((tokeniser ?? new Tokeniser()).Tokenise(text));
        }

        /// <summary>
        /// Converts a stream into an enumeration of tokens.
        /// </summary>
        /// <param name="stream">The stream of text</param>
        /// <param name="encoding">An optional encoding</param>
        /// <param name="tokeniser">An optional tokeniser</param>
        /// <returns>An enumeration of <see cref="IToken"/></returns>
        public static IEnumerable<IToken> Tokenise(this Stream stream, Encoding encoding = null, ITokeniser tokeniser = null)
        {
            return (new StreamTokeniser(encoding, tokeniser).Tokenise(stream));
        }
    }
}