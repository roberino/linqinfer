using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Text.VectorExtraction;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using LinqInfer.Text.Analysis;

namespace LinqInfer.Text
{
    public static class TextExtensions
    {
        /// <summary>
        /// Returns a corpus from a text reader
        /// </summary>
        public static ICorpus CreateCorpus(this TextReader textReader, ITokeniser tokeniser = null)
        {
            return new TextReaderToCorpusAdapter(tokeniser).CreateCorpus(textReader);
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
                    .CreatePipeline(featureExtractor)
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