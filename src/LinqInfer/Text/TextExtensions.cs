using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Text.Http;
using LinqInfer.Text.VectorExtraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class TextExtensions
    {
        public static Task<HttpDocument> OpenAsHtmlTokenDocumentAsync(this Uri rootUri)
        {
            return new HttpDocumentServices().GetDocumentAsync(rootUri);
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="docData">An exported multilayer network</returns>
        /// <param name="tokeniser">An optional tokeniser</param>
        public static IDynamicClassifier<TClass, string> OpenAsTextualMultilayerNetworkClassifier<TClass>(
            this BinaryVectorDocument docData, ITokeniser tokeniser = null) where TClass : IEquatable<TClass>
        {
            var featureExtractor = new TextVectorExtractor();
            var t = tokeniser ?? new Tokeniser();
            var objFeatureExtractor = featureExtractor.CreateObjectTextVectoriser<string>(t.Tokenise);

            return MlnExtensions.OpenAsMultilayerNetworkClassifier<string, TClass>(docData, objFeatureExtractor);
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="docData">An exported multilayer network</returns>
        public static IDynamicClassifier<TClass, TInput> OpenAsTextualMultilayerNetworkClassifier<TClass, TInput>(
            this BinaryVectorDocument docData)
            where TClass : IEquatable<TClass>
            where TInput : class
        {
            var featureExtractor = new TextVectorExtractor();
            var index = new DocumentIndex();
            var tokeniser = new ObjectTextExtractor<TInput>(index.Tokeniser);
            var objFeatureExtractor = featureExtractor.CreateObjectTextVectoriser<TInput>(tokeniser.CreateObjectTextTokeniser());

            return MlnExtensions.OpenAsMultilayerNetworkClassifier<TInput, TClass>(docData, objFeatureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        /// <param name="data">A queryable set of strings</param>
        /// <param name="maxVectorSize">The maximum size of the extracted vector</param>
        /// <returns></returns>
        public static FeatureProcessingPipeline<string> CreateTextFeaturePipeline(this IQueryable<string> data, int maxVectorSize = 128)
        {
            var identityFunc = new Func<string, string>((x => x == null ? "" : x.GetHashCode().ToString()));

            var index = new DocumentIndex();
            var tokeniser = new Tokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(identityFunc(x), tokeniser.Tokenise(x)));

            index.IndexDocuments(docs);

            return new FeatureProcessingPipeline<string>(data, index.CreateVectorExtractor<string>(tokeniser.Tokenise, maxVectorSize));
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        public static FeatureProcessingPipeline<TokenisedTextDocument> CreateTextFeaturePipeline(this IQueryable<TokenisedTextDocument> documents, ISemanticSet keyTerms)
        {
            var ve = new TextVectorExtractor(keyTerms.Words, 0, false);

            return new FeatureProcessingPipeline<TokenisedTextDocument>(documents, ve.CreateObjectTextVectoriser<TokenisedTextDocument>(s => s.Tokens));
        }

        /// <summary>
        /// Creates a feature extractor which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        public static IFloatingPointFeatureExtractor<TokenisedTextDocument> CreateTextFeatureExtractor(this ISemanticSet keyTerms, ITokeniser tokeniser = null)
        {
            if (tokeniser == null) tokeniser = new Tokeniser();

            var ve = new TextVectorExtractor(keyTerms.Words, 0, false);

            return ve.CreateObjectTextVectoriser<TokenisedTextDocument>(s => s.Tokens);
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        public static FeatureProcessingPipeline<string> CreateTextFeaturePipeline(this IQueryable<string> data, ISemanticSet keyTerms, ITokeniser tokeniser = null)
        {
            if (tokeniser == null) tokeniser = new Tokeniser();

            var ve = new TextVectorExtractor(keyTerms.Words, 0, false);

            return new FeatureProcessingPipeline<string>(data, ve.CreateObjectTextVectoriser<string>(s => tokeniser.Tokenise(s)));
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data</param>
        /// <param name="identityFunc">A function which determines how the data will be indexed</param>
        /// <param name="maxVectorSize">The maximum size of the extracted vector</param>
        /// <returns></returns>
        public static FeatureProcessingPipeline<T> CreateTextFeaturePipeline<T>(this IQueryable<T> data, Func<T, string> identityFunc = null, int maxVectorSize = 128) where T : class
        {
            if (identityFunc == null) identityFunc = (x => x == null ? "" : x.GetHashCode().ToString());

            var index = new DocumentIndex();
            var tokeniser = new ObjectTextExtractor<T>(index.Tokeniser);
            var objtokeniser = tokeniser.CreateObjectTextTokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(identityFunc(x), objtokeniser(x)));

            index.IndexDocuments(docs);

            return new FeatureProcessingPipeline<T>(data, index.CreateVectorExtractorByDocumentKey(objtokeniser, maxVectorSize));
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// using the supplied keywords
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="keywords">The keywords to use for constructing a vector</param>
        /// <returns></returns>
        public static FeatureProcessingPipeline<T> CreateTextFeaturePipeline<T>(this IQueryable<T> data, params string[] keywords) where T : class
        {
            var tokeniser = new Tokeniser();
            var otokeniser = new ObjectTextExtractor<T>(tokeniser);
            var objtokeniser = otokeniser.CreateObjectTextTokeniser();
            var vectorExtractor = new TextVectorExtractor(keywords, 100, false);

            var pipeline = new FeatureProcessingPipeline<T>(data, vectorExtractor.CreateObjectTextVectoriser(objtokeniser));

            pipeline.NormaliseData();

            return pipeline;
        }

        /// <summary>
        /// Creates a semantic neural network classifier which
        /// uses term frequency over known classifications of objects.
        /// </summary>
        /// <typeparam name="T">The input token type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="classifyingFunction">A function which will return a class label for an object</param>
        /// <param name="maxVectorSize">The maximum number of terms (words) used to classify object</param>
        /// <returns>An object classifier</returns>
        public static IObjectClassifier<string, T> CreateSemanticClassifier<T>(this IQueryable<T> data, Expression<Func<T, string>> classifyingFunction, int maxVectorSize = 128) where T : class
        {
            var index = new DocumentIndex();
            var cf = classifyingFunction.Compile();
            var tokeniser = new ObjectTextExtractor<T>(index.Tokeniser);
            var objtokeniser = tokeniser.CreateObjectTextTokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(cf(x), objtokeniser(x)));

            index.IndexDocuments(docs);

            var pipeline = new FeatureProcessingPipeline<T>(data, index.CreateVectorExtractorByDocumentKey(objtokeniser, maxVectorSize));

            return pipeline.ToMultilayerNetworkClassifier(classifyingFunction).Execute();
        }

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

        /// <summary>
        /// Parses a stream as a HTML document, converting it into an enumeration of <see cref="XNode"/>
        /// </summary>
        /// <param name="stream">The HTML stream</param>
        /// <param name="encoding">The text encoding</param>
        /// <returns></returns>
        public static IEnumerable<XNode> OpenAsHtml(this Stream stream, Encoding encoding = null)
        {
            using (var reader = encoding == null ? new StreamReader(stream, true) : new StreamReader(stream, encoding))
            {
                return new HtmlParser().Parse(reader);
            }
        }

        /// <summary>
        /// Parses a stream as a HTML document, converting it into an <see cref="XDocument"/>
        /// </summary>
        /// <param name="stream">The HTML stream</param>
        /// <param name="encoding">The text encoding</param>
        /// <returns>An <see cref="XDocument"/></returns>
        public static XDocument OpenAsHtmlDocument(this Stream stream, Encoding encoding = null)
        {
            using (var reader = encoding == null ? new StreamReader(stream, true) : new StreamReader(stream, encoding))
            {
                var nodes = new HtmlParser(true).Parse(reader).ToList();

                if (nodes.Count == 1 && nodes.Single().NodeType == System.Xml.XmlNodeType.Element)
                {
                    return new XDocument(nodes.Single());
                }
                else
                {
                    return new XDocument(new XElement("html", new XElement("body", nodes)));
                }
            }
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
                .Join(search.SearchInternal(q), o => o.Key, i => i.Key, (o, i) => new KeyValuePair<XDocument, float>(o.Value, i.Value));
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

            return q => search.SearchInternal(q);
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

            return q => search.SearchInternal(q);
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

            return q => search.SearchInternal(q);
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

			store.Restore(key, search);

            return q => search.SearchInternal(q);
        }
    }
}