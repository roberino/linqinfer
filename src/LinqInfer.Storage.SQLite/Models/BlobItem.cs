using System;
using System.IO;

namespace LinqInfer.Storage.SQLite.Models
{
    internal class BlobItem : Entity
    {
        public BlobItem()
        {
            Created = DateTime.UtcNow;
        }

        public Stream Write()
        {
            return new StreamWrapper(s => Data = s.ToArray());
        }

        public Stream Read()
        {
            return Data == null ? new MemoryStream() : new MemoryStream(Data);
        }

        public DateTime Created { get; set; }
        public string Key { get; set; }
        public string TypeName { get; set; }
        public byte[] Data { get; set; }

        private class StreamWrapper : MemoryStream
        {
            private readonly Action<MemoryStream> _onDispose;

            public StreamWrapper(Action<MemoryStream> onDispose)
            {
                _onDispose = onDispose;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _onDispose(this);
                base.Dispose(disposing);
            }
        }
    }
}
