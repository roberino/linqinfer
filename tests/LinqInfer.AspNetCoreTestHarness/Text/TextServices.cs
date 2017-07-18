using LinqInfer.AspNetCore;
using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class TextServices
    {
        private readonly IObjectSerialiser _serialiser;

        public TextServices(IHttpApiBuilder apiBuilder, IObjectSerialiser serialiser = null)
        {
            _serialiser = serialiser ?? new JsonObjectSerialiser();

            apiBuilder.Bind("/text/index/{indexName}", Verb.Post).To("", CreateIndex);
            apiBuilder.Bind("/text/index/{indexName}", Verb.Put).To("", OverwriteIndex);
            apiBuilder.Bind("/text/index/{indexName}", Verb.Get).To("", GetIndex);
            apiBuilder.Bind("/text/index/{indexName}/$features?maxVectorSize=256&transform=x", Verb.Get).To(new FeatureExtractRequest(), ExtractVectors);
            apiBuilder.Bind("/text/index/{indexName}/$classifiers/{classifierName}/$create", Verb.Post).To(new ClassifierRequest(), CreateClassifier);
            apiBuilder.Bind("/text/index/{indexName}/$classifiers/{classifierName}/$classify", Verb.Post).To(new ClassifyRequest(), ClassifyText);
            apiBuilder.Bind("/text/index/{indexName}/{documentId}", Verb.Post).To(new DocumentIndexRequest(), AddDocument);
            apiBuilder.Bind("/text/index/{indexName}/{documentId}", Verb.Get).To(new DocumentIndexRequest(), GetDocument);
        }

        private static async Task<IEnumerable<ColumnVector1D>> ExtractVectors(FeatureExtractRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(GetAllDocuments(request.IndexName).AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            return pipeline.ExtractVectors();
        }

        private static IEnumerable<TokenisedTextDocument> GetAllDocuments(string indexName)
        {
            var indexDir = GetFile(indexName, false).Directory;

            foreach (var file in indexDir.GetFiles("*.doc.xml"))
            {
                using (var fs = file.OpenRead())
                {
                    yield return TokenisedTextDocument.FromXmlStream(fs);
                }
            }
        }

        private static async Task<ClassifierRequest> CreateClassifier(ClassifierRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(GetAllDocuments(request.IndexName).AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            var trainingSet = pipeline.AsTrainingSet(d => d.Attributes[request.ClassAttributeName].ToString());

            var classifier = await trainingSet.ToMultilayerNetworkClassifier().ExecuteAsync();

            var file = GetFile(request.IndexName, false, false, request.ClassifierName + ".classifier.xml");

            if (file.Exists) file.Delete();

            using (var fs = file.OpenWrite())
            {
                classifier.ToVectorDocument().ExportAsXml().Save(fs);
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return request;
        }

        private static async Task<IEnumerable<ClassifyResult<string>>> ClassifyText(ClassifyRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var doc = new TokenisedTextDocument(Guid.NewGuid().ToString(), request.Text.Tokenise(index.Tokeniser));

            var fe = TextExtensions.CreateTextFeatureExtractor(index.ExtractKeyTerms(request.MaxVectorSize));

            var file = GetFile(request.IndexName, false, false, request.ClassifierName + ".classifier.xml");

            IDynamicClassifier<string, TokenisedTextDocument> classifier;

            using (var fs = file.OpenRead())
            {
                var xml = XDocument.Load(fs);
                var dataDoc = new BinaryVectorDocument(xml);

                classifier = dataDoc.OpenAsMultilayerNetworkClassifier<TokenisedTextDocument, string>(fe);
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return classifier.Classify(doc);
        }

        private static Task<DocumentIndexViewModel> CreateIndex(string indexName)
        {
            return CreateIndex(indexName, false);
        }

        private static Task<DocumentIndexViewModel> OverwriteIndex(string indexName)
        {
            return CreateIndex(indexName, true);
        }

        private static async Task<DocumentIndexViewModel> GetIndex(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            return new DocumentIndexViewModel(index, indexName);
        }

        private static Task<DocumentIndexViewModel> CreateIndex(string indexName, bool replace)
        {
            var file = GetFile(indexName);

            if (file.Exists) throw new InvalidOperationException();

            var index = Enumerable.Empty<TokenisedTextDocument>().CreateIndex();

            using (var fs = file.OpenWrite())
            {
                index.ExportAsXml().Save(fs);
            }

            return Task.FromResult(new DocumentIndexViewModel(index, indexName));
        }

        private Task<TokenisedTextDocument> GetDocument(DocumentIndexRequest request)
        {
            var docFile = GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml");

            using (var fs = docFile.OpenRead())
            {
                var tokenisedTextDoc = TokenisedTextDocument.FromXmlStream(fs);

                return Task.FromResult(tokenisedTextDoc);
            }
        }
        private async Task<DocumentIndexRequest> AddDocument(DocumentIndexRequest request)
        {
            var indexFile = GetFile(request.IndexName);

            if (!indexFile.Exists) throw new InvalidOperationException("Index missing: " + request.IndexName);

            var docFile = GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml");

            var index = await GetIndexInternal(request.IndexName);

            var doc = new TokenisedTextDocument(request.DocumentId, request.Text.Tokenise(index.Tokeniser));

            if (request.Attributes != null)
            {
                foreach (var attr in request.Attributes)
                {
                    doc.Attributes[attr.Key] = attr.Value;
                }
            }

            index.IndexDocument(doc);

            using(var fs = docFile.OpenWrite())
            {
                doc.ExportAsXml().Save(fs);
            }

            using (var fs = indexFile.OpenWrite())
            {
                index.ExportAsXml().Save(fs);
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return request;
        }

        private static Task<IDocumentIndex> GetIndexInternal(string indexName)
        {
            using (var fs = GetFile(indexName, false).OpenText())
            {
                var docIndexXml = XDocument.Load(fs);
                var docIndex = docIndexXml.OpenAsIndex();
                return Task.FromResult(docIndex);
            }
        }

        private static FileInfo GetFile(string storeName, bool createDir = true, bool isMetaFile = true, string name = "index.xml")
        {
            if (storeName.Any(c => !char.IsLetterOrDigit(c))) throw new ArgumentException(storeName);

            var cd = Directory.GetCurrentDirectory();

            var file = new FileInfo(Path.Combine(cd, storeName + Path.DirectorySeparatorChar, (isMetaFile ? "_" : "") + name));

            if (createDir && !file.Directory.Exists) file.Directory.Create();

            return file;
        }
    }
}