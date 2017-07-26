﻿using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Microservices.Resources;
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
        private readonly IVirtualFileStore _storage;

        public TextServices(IVirtualFileStore storage)
        {
            _storage = storage;
        }

        public TextServices(DirectoryInfo dataDir = null)
        {
            _storage = new FileStorage(dataDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "App_Data")));
        }

        public void Register(IHttpApiBuilder apiBuilder)
        {
            apiBuilder.Bind("/text/indexes/{indexName}").ToMany(b =>
            {
                b.UsingMethod(Verb.Post).To("", CreateIndex);
                b.UsingMethod(Verb.Put).To("", OverwriteIndex);
                b.UsingMethod(Verb.Get).To("", GetIndex);
                b.UsingMethod(Verb.Delete).To("", DeleteIndex);
            });

            apiBuilder.Bind("/text/indexes/{indexName}/key-terms", Verb.Get).To("", GetKeyTerms);
            apiBuilder.Bind("/text/indexes/{indexName}/search?q=a", Verb.Get).To(new SearchRequest(), Search);

            apiBuilder.Bind("/text/indexes/{indexName}/features/map?maxVectorSize=256&transform=x", Verb.Get).To(new FeatureExtractRequest(), GetSofm);
            apiBuilder.Bind("/text/indexes/{indexName}/features?maxVectorSize=256&transform=x", Verb.Get).To(new FeatureExtractRequest(), ExtractVectors);
            apiBuilder.Bind("/text/indexes/{indexName}/classifiers/{classifierName}/$create", Verb.Post).To(new ClassifierRequest(), CreateClassifier);
            apiBuilder.Bind("/text/indexes/{indexName}/classifiers/{classifierName}/$classify", Verb.Post).To(new ClassifyRequest(), ClassifyText);

            apiBuilder.Bind("/text/indexes/{indexName}/documents", Verb.Get).To("", ListDocuments);

            apiBuilder.Bind("/text/indexes/{indexName}/documents/{documentId}").ToMany(b =>
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
            var docs = await GetAllDocuments(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(docs.AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);
            
            return pipeline.ExtractVectors();
        }

        private async Task<FeatureMap<TokenisedTextDocument>> GetSofm(FeatureExtractRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);
            var docs = await GetAllDocuments(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(docs.AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            return pipeline.ToSofm().Execute();
        }

        private async Task<IList<TokenisedTextDocument>> GetAllDocuments(string indexName, int maxDocs = 1000)
        {
            var files = await GetDocumentFiles(indexName);

            int i = 0;

            var list = new List<TokenisedTextDocument>();

            foreach (var file in files)
            {
                using (var fs = await file.ReadData())
                {
                    list.Add(TokenisedTextDocument.FromXmlStream(fs));
                }

                if (maxDocs == i++) break;
            }

            return list;
        }

        private async Task<List<IVirtualFile>> GetDocumentFiles(string indexName)
        {
            return (await _storage.GetContainer(indexName).ListFiles()).Where(f => f.Name.EndsWith(".doc.xml")).ToList();
        }

        private async Task<ResourceList<string>> ListDocuments(string indexName)
        {
            var files = await GetDocumentFiles(indexName);

            return files.Select(f => $"/text/indexes/{indexName}/documents/{f.Name.Substring(0, f.Name.Length - 8)}").ToResourceList();
        }

        private async Task<ClassifierRequest> CreateClassifier(ClassifierRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);
            var docs = await GetAllDocuments(request.IndexName);

            var pipeline = TextExtensions.CreateTextFeaturePipeline(docs.AsQueryable(), index.ExtractKeyTerms(request.MaxVectorSize));

            await request.Apply(pipeline);

            var trainingSet = pipeline.AsTrainingSet(d => d.Attributes[request.ClassAttributeName].ToString());

            var classifier = await trainingSet.ToMultilayerNetworkClassifier(request.ErrorTolerance.GetValueOrDefault(0.1f)).ExecuteAsync();

            var file = await GetFile(request.IndexName, false, false, request.ClassifierName + ".classifier.xml");

            if (file.Exists) await file.Delete();

            using (var fs = file.GetWriteStream())
            {
                classifier.ToVectorDocument().ExportAsXml().Save(fs);
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return request;
        }

        private async Task<ResourceList<ClassifyResult<string>>> ClassifyText(ClassifyRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            var doc = new TokenisedTextDocument(Guid.NewGuid().ToString(), request.Text.Tokenise(index.Tokeniser));

            var fe = TextExtensions.CreateTextFeatureExtractor(index.ExtractKeyTerms(request.MaxVectorSize));

            IDynamicClassifier<string, TokenisedTextDocument> classifier;

            using (var file = await GetFile(request.IndexName, false, false, request.ClassifierName + ".classifier.xml"))
            {
                using (var fs = await file.ReadData())
                {
                    var xml = XDocument.Load(fs);
                    var dataDoc = new BinaryVectorDocument(xml);

                    classifier = dataDoc.OpenAsMultilayerNetworkClassifier<TokenisedTextDocument, string>(fe);
                }

                await file.CommitWrites();
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return classifier.Classify(doc).ToResourceList();
        }

        private async Task<DocumentIndexView> DeleteIndex(string indexName)
        {
            await GetStorageContainer(indexName).Delete();

            return new DocumentIndexView(null, indexName);
        }

        private Task<DocumentIndexView> CreateIndex(string indexName)
        {
            return CreateIndex(indexName, false);
        }

        private Task<DocumentIndexView> OverwriteIndex(string indexName)
        {
            return CreateIndex(indexName, true);
        }

        private async Task<DocumentIndexView> GetIndex(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            var indexView = new DocumentIndexView(index, indexName);

            indexView.Links["documents"] = $"/text/indexes/{indexName}/documents";

            return indexView;
        }

        private async Task<ResourceList<SearchResult>> Search(SearchRequest request)
        {
            var index = await GetIndexInternal(request.IndexName);

            return index.Search(request.Q).ToResourceList($"index=/text/indexes/{request.IndexName}");
        }

        private async Task<ISemanticSet> GetKeyTerms(string indexName)
        {
            var index = await GetIndexInternal(indexName);

            return index.ExtractKeyTerms(500);
        }

        private async Task<DocumentIndexView> CreateIndex(string indexName, bool replace)
        {
            using (var file = await GetFile(indexName))
            {

                if (file.Exists) throw new InvalidOperationException();

                var index = Enumerable.Empty<TokenisedTextDocument>().CreateIndex();

                using (var fs = file.GetWriteStream())
                {
                    index.ExportAsXml().Save(fs);
                }

                await file.CommitWrites();

                var indexView = new DocumentIndexView(index, indexName);

                indexView.Links["documents"] = $"/text/indexes/{indexName}/documents";

                return indexView;
            }
        }

        private async Task<TokenisedTextDocument> GetDocument(DocumentIndexRequest request)
        {
            using (var docFile = await GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml"))
            {
                using (var fs = await docFile.ReadData())
                {
                    var tokenisedTextDoc = TokenisedTextDocument.FromXmlStream(fs);

                    return tokenisedTextDoc;
                }
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
            var indexFile = await GetFile(request.IndexName);

            if (!indexFile.Exists) throw new InvalidOperationException("Index missing: " + request.IndexName);

            if (request.DocumentId == null || request.DocumentId == "-") request.DocumentId = Guid.NewGuid().ToString("N");

            var docFile = await GetFile(request.IndexName, false, false, request.DocumentId + ".doc.xml");

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

            using(var fs = docFile.GetWriteStream())
            {
                doc.ExportAsXml().Save(fs);
            }

            using (var fs = indexFile.GetWriteStream())
            {
                index.ExportAsXml().Save(fs);
            }

            using (docFile)
            using (indexFile)
            {
                await docFile.CommitWrites();
                await indexFile.CommitWrites();
            }

            request.LastUpdated = DateTime.UtcNow;
            request.Confirmed = true;

            return request;
        }

        private async Task<IDocumentIndex> GetIndexInternal(string indexName)
        {
            using (var indexFile = await GetFile(indexName, false))
            {
                using (var fs = await indexFile.ReadData())
                {
                    var docIndexXml = XDocument.Load(fs);
                    var docIndex = docIndexXml.OpenAsIndex();
                    return docIndex;
                }
            }
        }

        private async Task<IVirtualFile> GetFile(string storeName, bool createDir = true, bool isMetaFile = true, string name = "index.xml")
        {
            if (name.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_')) throw new ArgumentException(name);

            var file = await GetStorageContainer(storeName).GetFile((isMetaFile ? "_" : "") + name);

            return file;
        }

        private IVirtualFileStore GetStorageContainer(string storeName)
        {
            if (storeName.Any(c => !char.IsLetterOrDigit(c))) throw new ArgumentException(storeName);

            return _storage.GetContainer(storeName);
        }
    }
}