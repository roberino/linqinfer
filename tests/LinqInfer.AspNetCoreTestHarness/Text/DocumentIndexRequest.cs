using System;
using System.Collections.Generic;

namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class DocumentIndexRequest : RequestBase
    {
        public string DocumentId { get; set; }

        public string IndexName { get; set; }

        public string Text { get; set; }

        public Uri SourceUrl { get; set; }

        public IDictionary<string, object> Attributes { get; set; }
    }
}