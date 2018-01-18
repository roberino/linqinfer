using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    internal sealed class HttpDocumentCrawler : IDisposable
    {
        private const int MaxVisited = 350;

        private readonly HttpDocumentClient _docClient;
        private readonly HashSet<Uri> _visited;

        internal HttpDocumentCrawler(
            HttpDocumentClient documentClient)
        {
            _docClient = documentClient;
            _visited = new HashSet<Uri>();
        }

        public IEnumerable<Uri> VisitedUrls => _visited;

        public IEnumerable<Task<IList<HttpDocument>>> CrawlDocuments(Uri rootUri, HttpDocumentCrawlerOptions options = null)
        {
            if (options == null) options = new HttpDocumentCrawlerOptions();

            var pending = new Queue<IList<Uri>>();

            pending.Enqueue(new[] { rootUri });
            int count = 1;

            var f = new Func<Task<IList<HttpDocument>>>(async () =>
            {
                var docs = new List<HttpDocument>();

                var links = pending.Dequeue().Take(options.MaxNumberOfDocuments - count);

                foreach (var child in await _docClient.FollowLinks(links, options.TargetElement))
                {
                    if ((options.DocumentFilter?.Invoke(child)).GetValueOrDefault(true))
                    {
                        docs.Add(child);

                        count++;

                        pending.Enqueue(child.Links.Select(l => l.Url).Where(options.LinkFilter).ToList());
                    }

                    if (count >= options.MaxNumberOfDocuments) break;
                }

                return docs;
            });

            while (count < options.MaxNumberOfDocuments && pending.Any())
            {
                yield return f();
            }
        }

        public void Dispose()
        {
        }
    }
}