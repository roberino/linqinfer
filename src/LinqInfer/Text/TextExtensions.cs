using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Text.VectorExtraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Utility;

namespace LinqInfer.Text
{
    public static class TextExtensions
    {
        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="docData">An exported multilayer network</returns>
        /// <param name="tokeniser">An optional tokeniser</param>
        public static IDynamicClassifier<TClass, string> OpenAsTextualMultilayerNetworkClassifier<TClass>(
            this PortableDataDocument docData, ITokeniser tokeniser = null) where TClass : IEquatable<TClass>
        {
            var featureExtractor = new TextDataExtractor();
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
            this PortableDataDocument docData)
            where TClass : IEquatable<TClass>
            where TInput : class
        {
            var featureExtractor = new TextDataExtractor();
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
            var ve = new TextDataExtractor(keyTerms.Words, 0, false);

            return new FeatureProcessingPipeline<TokenisedTextDocument>(documents, ve.CreateObjectTextVectoriser<TokenisedTextDocument>(s => s.Tokens));
        }

        /// <summary>
        /// Creates a feature extractor which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        public static IFloatingPointFeatureExtractor<TokenisedTextDocument> CreateTextFeatureExtractor(this ISemanticSet keyTerms, ITokeniser tokeniser = null)
        {
            if (tokeniser == null) tokeniser = new Tokeniser();

            var ve = new TextDataExtractor(keyTerms.Words, 0, false);

            return ve.CreateObjectTextVectoriser<TokenisedTextDocument>(s => s.Tokens);
        }

        /// <summary>
        /// Creates a feature processing pipeline which extracts sematic vectors
        /// based on term frequency.
        /// </summary>
        public static FeatureProcessingPipeline<string> CreateTextFeaturePipeline(this IQueryable<string> data, ISemanticSet keyTerms, ITokeniser tokeniser = null)
        {
            if (tokeniser == null) tokeniser = new Tokeniser();

            var ve = new TextDataExtractor(keyTerms.Words, 0, false);

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
            var vectorExtractor = new TextDataExtractor(keywords, 100, false);

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
        public static IObjectClassifier<string, T> CreateSemanticClassifier<T>(this IQueryable<T> data,
            Expression<Func<T, string>> classifyingFunction, int maxVectorSize = 128) where T : class
        {
            var index = new DocumentIndex();
            var cf = classifyingFunction.Compile();
            var tokeniser = new ObjectTextExtractor<T>(index.Tokeniser);
            var objtokeniser = tokeniser.CreateObjectTextTokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(cf(x), objtokeniser(x)));

            index.IndexDocuments(docs);

            var featureExtractor = index.CreateVectorExtractorByDocumentKey(objtokeniser, maxVectorSize);

            var outputs = data.GroupBy(classifyingFunction).Select(g => g.Key).ToArray();

            var trainingSet =
                data.AsAsyncEnumerator()
                    .CreatePipeine(featureExtractor)
                    .AsTrainingSet(classifyingFunction, outputs);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(
                b =>
                {
                    b.ConfigureSoftmaxNetwork(
                        featureExtractor.VectorSize * 2,
                        lp => { lp.LearningRate = 0.01; });
                }
            );

            trainingSet.RunAsync(CancellationToken.None, 100).ConfigureAwait(true).GetAwaiter().GetResult();

            return classifier;
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