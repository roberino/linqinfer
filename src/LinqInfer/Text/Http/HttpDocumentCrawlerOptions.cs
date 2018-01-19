using LinqInfer.Utility;
using System;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public sealed class HttpDocumentCrawlerOptions
    {
        private readonly static HttpDocumentCrawlerOptions _default;

        public int BatchSize { get; set; } = 10;
        public Func<Uri, bool> LinkFilter { get; set; } = _ => true;
        public Func<HttpDocument, bool> DocumentFilter { get; set; } = _ => true;
        public int MaxNumberOfDocuments { get; set; } = 50;
        public Func<XElement, XElement> TargetElement { get; set; } = x => x;

        internal HttpDocumentCrawlerOptions CreateValid()
        {
            ArgAssert.AssertGreaterThanZero(MaxNumberOfDocuments, nameof(MaxNumberOfDocuments));
            ArgAssert.AssertGreaterThanZero(BatchSize, nameof(BatchSize));

            return new HttpDocumentCrawlerOptions()
            {
                DocumentFilter = DocumentFilter ?? _default.DocumentFilter,
                LinkFilter = LinkFilter ?? _default.LinkFilter,
                TargetElement = TargetElement ?? _default.TargetElement,
                MaxNumberOfDocuments = MaxNumberOfDocuments,
                BatchSize = BatchSize
            };
        }
    }
}