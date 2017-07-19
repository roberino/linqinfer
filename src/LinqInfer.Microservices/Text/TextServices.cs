using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Text;
using LinqInfer.Text.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LinqInfer.Microservices.Text
{
    public class TextServices
    {
        public void Register(IHttpApiBuilder apiBuilder)
        {
            apiBuilder.Bind("/text/indexes/{indexName}").ToMany(b =>
            {
                b.UsingMethod(Verb.Post).To("", CreateIndex);
                b.UsingMethod(Verb.Put).To("", OverwriteIndex);
                b.UsingMethod(Verb.Get).To("", GetIndex);
            });

            apiBuilder.Bind("/text/indexes/{indexName}/$keyTerms", Verb.Get).To("", GetKeyTerms);
            apiBuilder.Bind("/text/indexes/{indexName}/$search?q=a", Verb.Get).To(new SearchRequest(), Search);

            apiBuilder.Bind("/text/indexes/{indexName}/$features/$map?maxVectorSize=256&transform=x", Verb.Get).To(new FeatureExtractRequest(), GetSofm);
            apiBuilder.Bind("/text/indexes/{indexName}/$features?maxVectorSize=256&transform=x", Verb.Get).To(new FeatureExtractRequest(), ExtractVectors);
            apiBuilder.Bind("/text/indexes/{indexName}/$classifiers/{classifierName}/$create", Verb.Post).To(new ClassifierRequest(), CreateClassifier);
            apiBuilder.Bind("/text/indexes/{indexName}/$classifiers/{classifierName}/$classify", Verb.Post).To(new ClassifyRequest(), ClassifyText);

            apiBuilder.Bind("/text/indexes/{indexName}/{documentId}").ToMany(b =>
            {
                b.UsingMethod(Verb.Post).To(new DocumentIndexRequest(), AddDocument);
                b.UsingMethod(Verb.Put).To(new DocumentIndexRequest(), UpdateDocument);
                b.UsingMethod(Verb.Patch).To(new DocumentIndexRequest(), PatchDocument);
                b.UsingMethod(Verb.Get).To(new DocumentIndexRequest(), GetDocument);
            });
        }

        private async Task<IEnumerable<ColumnVector1D>> ExtractVectors(FeatureExtractRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(GetAllDocuments(request.IndexName).AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            return pipeline.ExtractVectors();
        }

        private async Task<FeatureMap<TokenisedTextDocument>> GetSofm(FeatureExtractRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(GetAllDocuments(request.IndexName).AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            return pipeline.ToSofm().Execute();
        }

        private IEnumerable<TokenisedTextDocument> GetAllDocuments(string indexName)
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

        private async Task<ClassifierRequest> CreateClassifier(ClassifierRequest request)
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

        private async Task<IEnumerable<ClassifyResult<string>>> ClassifyText(ClassifyRequest request)
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

        private Task<DocumentIndexViewModel> CreateIndex(string indexName)
        {
            return CreateIndex(indexName, false);
        }

        private Task<DocumentIndexViewModel> OverwriteIndex(string indexName)
        {
            return CreateIndex(indexName, true);
        }

        private async Task<DocumentIndexViewModel> GetIndex(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            return new DocumentIndexViewModel(index, indexName);
        }

        private async Task<IEnumerable<SearchResult>> Search(SearchRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            return index.Search(request.Q);
        }

        private async Task<ISemanticSet> GetKeyTerms(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            return index.ExtractKeyTerms(25);
        }

        private Task<DocumentIndexViewModel> CreateIndex(string indexName, bool replace)
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

        private Task<DocumentIndexRequest> AddDocument(DocumentIndexRequest request)
        {
            return AddOrModifyDocument(request);
        }

        private Task<DocumentIndexRequest> UpdateDocument(DocumentIndexRequest request)
        {
            return AddOrModifyDocument(request, Verb.Put);
        }

        private Task<DocumentIndexRequest> PatchDocument(DocumentIndexRequest request)
        {
            return AddOrModifyDocument(request, Verb.Patch);
        }

        private async Task<DocumentIndexRequest> AddOrModifyDocument(DocumentIndexRequest request, Verb behaviour = Verb.Post)
        {
            var indexFile = GetFile(request.IndexName);

            if (!indexFile.Exists) throw new InvalidOperationException("Index missing: " + request.IndexName);

            if (request.DocumentId == null || request.DocumentId == "-") request.DocumentId = Guid.NewGuid().ToString("N");

            var docFile = GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml");

            if (docFile.Exists)
            {
                if (behaviour == Verb.Post) throw new InvalidOperationException("Doc exists (use PUT or PATCH to update): " + request.DocumentId);
            }
            else
            {
                if (behaviour == Verb.Patch) throw new InvalidOperationException("Doc missing (use POST to create): " + request.DocumentId);
            }

            var index = await GetIndexInternal(request.IndexName);

            TokenisedTextDocument doc;

            if (request.SourceUrl != null)
            {
                using (var docServices = new HttpDocumentServices())
                {
                    var hdoc = await docServices.GetDocument(request.SourceUrl);
                    
                    doc = new TokenisedTextDocument(request.DocumentId, hdoc.Tokens);

                    foreach (var md in hdoc.Metadata)
                    {
                        doc.Attributes[XmlConvert.EncodeLocalName(md.Key)] = md.Value;
                    }

                    foreach (var md in hdoc.Headers)
                    {
                        doc.Attributes[XmlConvert.EncodeLocalName(md.Key)] = string.Join(",", md.Value);
                    }

                    if (hdoc.Title != null) doc.Attributes["title"] = hdoc.Title;
                }
            }
            else
            {
                doc = new TokenisedTextDocument(request.DocumentId, request.Text.Tokenise(index.Tokeniser));
            }

            if (request.Attributes != null)
            {
                foreach (var attr in request.Attributes)
                {
                    doc.Attributes[attr.Key] = attr.Value;
                }
            }

            if (behaviour == Verb.Patch)
            {
                var existingDoc = await GetDocument(request);

                var mergeDoc = new TokenisedTextDocument(request.DocumentId, (request.Text == null && request.SourceUrl == null) ? existingDoc.Tokens : doc.Tokens);

                foreach (var attrib in existingDoc.Attributes)
                {
                    mergeDoc.Attributes[attrib.Key] = attrib.Value;
                }

                if (request.Attributes != null)
                {
                    foreach (var attrib in doc.Attributes)
                    {
                        mergeDoc.Attributes[attrib.Key] = attrib.Value;
                    }
                }

                doc = mergeDoc;
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