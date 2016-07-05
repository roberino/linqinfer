using System;
using System.Collections.Concurrent;
using System.IO;

namespace LinqInfer.Data
{
    /// <summary>
    /// In memory implementation of <see cref="IBlobStore"/>
    /// </summary>
    public class InMemoryBlobStore : BlobStoreBase
    {
        private readonly ConcurrentDictionary<string, Blob> _data;
        
        public InMemoryBlobStore()
        {
            _data = new ConcurrentDictionary<string, Blob>();
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

        public override void Dispose()
        {
            base.Dispose();

            _data.Clear();
        }
    }
}