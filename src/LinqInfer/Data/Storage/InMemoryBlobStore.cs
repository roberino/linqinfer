using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Storage
{
    /// <summary>
    /// In memory implementation of <see cref="IBlobStore"/>
    /// </summary>
    public class InMemoryBlobStore : BlobStoreBase
    {
        readonly ConcurrentDictionary<string, Blob> _data;
        
        public InMemoryBlobStore()
        {
            _data = new ConcurrentDictionary<string, Blob>();
        }

        public override Task<IEnumerable<string>> ListKeys<T>()
        {
            var baseKey = GetTypeKeyPart<T>();

            return Task.FromResult(_data.Keys.Where(d => d.StartsWith(baseKey + KeyDelimitter)).ToList().AsEnumerable());
        }

        protected override void OnWrite(string key, Stream stream)
        {
            var ms = (MemoryStream)stream;

            var blob = new Blob()
            {
                Created = DateTime.UtcNow,
                Data = ms.ToArray()
            };

            _data[key] = blob;
        }

        protected override Stream GetReadStream(string key)
        {
            var blob = _data[key];

            return new MemoryStream(blob.Data);
        }

        protected override Stream GetWriteStream(string key)
        {
            return new MemoryStream();
        }

        protected override bool RemoveBlob(string key)
        {
            Blob blob;
            return _data.TryRemove(key, out blob);
        }

        public override void Dispose()
        {
            base.Dispose();

            _data.Clear();
        }
    }
}