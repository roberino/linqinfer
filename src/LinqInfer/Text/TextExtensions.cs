using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Text.VectorExtraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class TextExtensions
    {
        public static FeatureProcessingPipline<T> CreateTextFeaturePipeline<T>(this IQueryable<T> data, Func<T, string> identityFunc = null, int maxVectorSize = 128) where T : class
        {
            if (identityFunc == null) identityFunc = (x => x == null ? "" : x.GetHashCode().ToString());

            var index = new DocumentIndex();
            var tokeniser = new ObjectTextExtractor<T>(index.Tokeniser);
            var objtokeniser = tokeniser.CreateObjectTextTokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(identityFunc(x), objtokeniser(x)));

            index.IndexDocuments(docs);
            
            return new FeatureProcessingPipline<T>(data, index.CreateVectorExtractor(objtokeniser, maxVectorSize));
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
            return (new StreamTokeniser(encoding, tokeniser).Tokensise(stream));
        }

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

            search.IndexDocuments(documents, keySelector);

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

            search.IndexDocuments(documents, keySelector);

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

            search.IndexDocuments(documents, keySelector);

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
