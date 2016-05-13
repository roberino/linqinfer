using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class TextExtensions
    {
        /// <summary>
        /// Searches documents using term frequency / inverse document frequency.
        /// </summary>
        /// <param name="documents">The documents to be searched</param>
        /// <param name="tokeniser">The optional tokeniser</param>
        /// <returns>Matching documents scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<XDocument, float>>> TermFrequencySearch(this IEnumerable<XDocument> documents, ITokeniser tokeniser = null)
        {
            var search = new TermFrequencyIdfDocumentSearch(tokeniser);

            search.IndexDocuments(documents.AsQueryable());

            return q => search.Search(documents.AsQueryable(), q);
        }
    }
}
