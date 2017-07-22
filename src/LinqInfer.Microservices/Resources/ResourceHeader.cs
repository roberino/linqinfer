using System;
using System.Collections.Generic;

namespace LinqInfer.Microservices.Resources
{
    public class ResourceHeader
    {
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public IDictionary<string, string> Links { get; internal set; } = new Dictionary<string, string>();
    }
}
