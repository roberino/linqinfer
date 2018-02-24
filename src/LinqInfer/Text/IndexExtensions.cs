using System.Collections.Generic;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class IndexExtensions
    {
        /// <summary>
        /// Returns a document index from an enumeration of tokenised documents
        /// </summary>
        /// <param name="tokenisedDocuments">An enumeration of tokenised documents</param>
        /// <returns>A document index</returns>
        public static IDocumentIndex CreateIndex(this IEnumerable<TokenisedTextDocument> tokenisedDocuments)
        {
            var index = new DocumentIndex();

            index.IndexDocuments(tokenisedDocuments);

            return index;
        }

        /// <summary>
        /// Returns a document index from exported xml
        /// </summary>
        /// <param name="documentIndexData">An XML exported index</param>
        /// <param name="tokeniser">An optional tokeniser</param>
        /// <returns>A document index</returns>
        public static IDocumentIndex OpenAsIndex(this XDocument documentIndexData, ITokeniser tokeniser = null)
        {
            var index = new DocumentIndex(tokeniser);

            index.ImportXml(documentIndexData);

            return index;
        }
    }
}
