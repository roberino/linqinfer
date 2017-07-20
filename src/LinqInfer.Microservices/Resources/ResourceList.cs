using System.Collections.Generic;

namespace LinqInfer.Microservices.Resources
{
    public class ResourceList<T> : ResourceHeader
    {
        public IList<T> Data { get; internal set; } = new List<T>();
    }
}