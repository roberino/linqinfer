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
        private readonly Func<Uri, bool> _linkFilter;

        internal HttpDocumentCrawler(
            HttpDocumentClient documentClient,
            Func<Uri, bool> linkFilter = null)
        {
            _docClient = documentClient;
            _linkFilter = linkFilter ?? (_ => true);
        }

        public IEnumerable<Task<IList<HttpDocument>>> CrawlDocuments(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var pending = new Queue<IList<Uri>>();

            pending.Enqueue(new[] { rootUri });
            int count = 1;

            var f = new Func<Task<IList<HttpDocument>>>(async () =>
            {
                var docs = new List<HttpDocument>();

                foreach (var child in await _docClient.FollowLinks(pending.Dequeue().Take(maxDocs - count), targetElement))
                {
                    if ((documentFilter?.Invoke(child)).GetValueOrDefault(true))
                    {
                        docs.Add(child);

                        count++;

                        pending.Enqueue(child.Links.Select(l => l.Url).Where(_linkFilter).ToList());
                    }

                    if (count >= maxDocs) break;
                }

                return docs;
            });

            while (count < maxDocs && pending.Any())
            {
                yield return f();
            }
        }

        public async Task CrawlDocuments(Uri rootUri, Action<HttpDocument> documentAction, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var pending = new Queue<IList<Uri>>();

            pending.Enqueue(new[] { rootUri });
            int count = 1;

            while (count < maxDocs && pending.Any())
            {
                foreach (var child in await _docClient.FollowLinks(pending.Dequeue().Take(maxDocs - count), targetElement))
                {
                    if ((documentFilter?.Invoke(child)).GetValueOrDefault(true))
                    {
                        documentAction(child);

                        count++;

                        pending.Enqueue(child.Links.Select(l => l.Url).Where(_linkFilter).ToList());
                    }

                    if (count >= maxDocs) break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}