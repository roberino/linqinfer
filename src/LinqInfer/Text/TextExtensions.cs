using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// <param name="keySelector">A function which returns a unique key for a document</param>
        /// <param name="tokeniser">The optional tokeniser</param>
        /// <returns>A function for matching documents scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<XDocument, float>>> TermFrequencySearch(this IEnumerable<XDocument> documents, Func<XDocument, string> keySelector, ITokeniser tokeniser = null)
        {
            var search = new DocumentIndex(tokeniser);

            search.IndexDocuments(documents.AsQueryable(), keySelector);

            return q => documents
                .Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d))
                .Join(search.Search(q), o => o.Key, i => i.Key, (o, i) => new KeyValuePair<XDocument, float>(o.Value, i.Value));
        }

        /// <summary>
        /// Creates an index function for a set of documents.
        /// </summary>
        /// <param name="documents">The documents to index</param>
        /// <param name="keySelector">A function which returns a unique key for a document</param>
        /// <param name="output">An optional stream to save the index to</param>
        /// <param name="tokeniser">An optional custom tokeniser</param>
        /// <returns>A function for matching document keys scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<string, float>>> TermFrequencyIndex(this IEnumerable<XDocument> documents, Func<XDocument, string> keySelector, Stream output = null, ITokeniser tokeniser = null)
        {
            var search = new DocumentIndex(tokeniser);

            search.IndexDocuments(documents.AsQueryable(), keySelector);

            if(output != null)
            {
                search.Save(output);
            }

            return q => search.Search(q);
        }

        /// <summary>
        /// Creates an index function for a set of documents.
        /// </summary>
        /// <param name="documents">The documents to index</param>
        /// <param name="keySelector">A function which returns a unique key for a document</param>
        /// <param name="output">An optional stream to save the index to</param>
        /// <param name="tokeniser">An optional custom tokeniser</param>
        /// <returns>A function for matching document keys scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<string, float>>> TermFrequencyIndex(this IEnumerable<XDocument> documents, Func<XDocument, string> keySelector, IBlobStore output, string key, ITokeniser tokeniser = null)
        {
            var search = new DocumentIndex(tokeniser);

            search.IndexDocuments(documents.AsQueryable(), keySelector);

            if (output != null)
            {
                output.Store(key, search);
            }

            return q => search.Search(q);
        }

        /// <summary>
        /// Opens a previously saved index from a stream.
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="tokeniser">An optional custom tokeniser</param>
        /// <returns>A function for matching document keys scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<string, float>>> OpenAsTermFrequencyIndex(this Stream input, ITokeniser tokeniser = null)
        {
            var search = new DocumentIndex(tokeniser);

            search.Load(input);

            return q => search.Search(q);
        }

        /// <summary>
        /// Opens a previously saved index from a blob store.
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="tokeniser">An optional custom tokeniser</param>
        /// <returns>A function for matching document keys scored by TF / IDF</returns>
        public static Func<string, IEnumerable<KeyValuePair<string, float>>> OpenAsTermFrequencyIndex(this IBlobStore store, string key, ITokeniser tokeniser = null)
        {
            var search = new DocumentIndex(tokeniser);

            var blob = store.Restore(key, search);

            return q => search.Search(q);
        }
    }
}
