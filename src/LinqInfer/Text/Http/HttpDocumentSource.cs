using LinqInfer.Data.Pipes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Http
{
    sealed class HttpDocumentSource
    {
        readonly HttpDocumentClient _docClient;
        readonly HttpDocumentCrawlerOptions _options;
        readonly ConcurrentDictionary<Uri, int> _visited;
        readonly ConcurrentQueue<Uri> _pending;

        int _batchCounter;
        int _docCounter;

        internal HttpDocumentSource(
            HttpDocumentClient documentClient,
            Uri rootUri,
            HttpDocumentCrawlerOptions options)
        {
            RootUri = rootUri;

            _docClient = documentClient;
            _visited = new ConcurrentDictionary<Uri, int>();
            _pending = new ConcurrentQueue<Uri>();
            _options = options.CreateValid();
        }

        public Uri RootUri { get; }

        public IEnumerable<Uri> VisitedUrls => _visited.Keys;

        public IAsyncEnumerator<HttpDocument> GetAsyncSource(Action onDispose = null)
        {
            return From.Func(LoadBatch, null, onDispose);
        }

        AsyncBatch<HttpDocument> LoadBatch(int i)
        {
            var completed = _visited.Any() && (!_pending.Any() || _docCounter >= _options.MaxNumberOfDocuments);

            Interlocked.Increment(ref _batchCounter);

            return new AsyncBatch<HttpDocument>(LoadNextItems(), completed, _batchCounter);
        }

        async Task<IList<HttpDocument>> LoadNextItems()
        {
            var batch = new List<HttpDocument>();

            async Task TryAddBatchItem(Uri uri)
            {
                var doc = await GetDocAsync(uri);

                if (_options.DocumentFilter(doc))
                {
                    batch.Add(doc);
                    Interlocked.Increment(ref _docCounter);
                }
            }

            if (_visited.Count == 0)
            {
                await TryAddBatchItem(RootUri);
            }

            while (batch.Count < _options.BatchSize && _pending.Any() && _docCounter < _options.MaxNumberOfDocuments)
            {
                if (_pending.TryDequeue(out Uri nextUri))
                {
                    await TryAddBatchItem(nextUri);
                }
            }

            return batch;
        }

        async Task<HttpDocument> GetDocAsync(Uri uri)
        {
            var doc = await _docClient.GetDocumentAsync(uri, _options.TargetElement);

            _visited.AddOrUpdate(uri, 1, (_, i) => i + 1);

            foreach (var link in doc
                .Links
                .Select(l => l.Url).Where(l => _options.LinkFilter(l) && !_visited.ContainsKey(l)))
            {
                _pending.Enqueue(link);
            }

            return doc;
        }
    }
}