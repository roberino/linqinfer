using LinqInfer.AspNetCore;
using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
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
            apiBuilder.Bind("/text/index/{indexName}", Verb.Get).To("", GetIndex);
            apiBuilder.Bind("/text/index/{indexName}/$features", Verb.Get).To("", ExtractVectors);
            apiBuilder.Bind("/text/index/{indexName}/{documentId}", Verb.Post).To(new DocumentIndexRequest(), AddDocument);
            apiBuilder.Bind("/text/index/{indexName}/{documentId}", Verb.Get).To(new DocumentIndexRequest(), GetDocument);
            apiBuilder.Bind("/text/classify/{classifierName}", Verb.Post).To(new ClassifierRequest(), CreateClassifier);            
        }

        private static async Task<IEnumerable<ColumnVector1D>> ExtractVectors(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(GetAllDocuments(indexName).AsQueryable(), index.ExtractKeyTerms(256));

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

            var pipeline = TextExtensions.CreateTextFeaturePipeline(Enumerable.Empty<string>().AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            var classifier = await pipeline.ToMultilayerNetworkClassifier(x => x).ExecuteAsync();

            var file = GetFile(request.ClassifierName);

            using (var fs = file.OpenWrite())
            {
                classifier.ToVectorDocument().ExportAsXml().Save(fs);
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return request;
        }

        private static Task<ISemanticSet> CreateIndex(string indexName)
        {
            var file = GetFile(indexName);

            var index = Enumerable.Empty<TokenisedTextDocument>().CreateIndex();

            using (var fs = file.OpenWrite())
            {
                index.ExportAsXml().Save(fs);
            }

            return Task.FromResult(index.ExtractTerms());
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
            var docFile = GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml");

            var index = await GetIndexInternal(request.IndexName);

            var doc = new TokenisedTextDocument(request.DocumentId, request.Text.Tokenise());

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

        private static async Task<ISemanticSet> GetIndex(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            return index.ExtractTerms();
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