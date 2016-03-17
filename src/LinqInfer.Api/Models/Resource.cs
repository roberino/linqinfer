using LinqInfer.Storage;
using System;
using System.Collections.Generic;

namespace LinqInfer.Api.Models
{
    public class Resource<T> where T : IStorageObject
    {
        public Resource(T item, Uri selfUri = null)
        {
            Views = new Dictionary<string, Uri>();
            Item = item;

            Views["self"] = selfUri ?? item.Uri;
        }

        public IDictionary<string, Uri> Views { get; private set; }

        public T Item { get; private set; }
    }
}