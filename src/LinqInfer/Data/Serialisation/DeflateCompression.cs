using System.IO;
using System.IO.Compression;

namespace LinqInfer.Data.Serialisation
{
    internal class DeflateCompression<T> : IBinaryPersistable where T : IBinaryPersistable
    {
        private readonly T _instance;

        public DeflateCompression(T instance)
        {
            _instance = instance;
        }

        public void Load(Stream input)
        {
            using (var cs = new DeflateStream(input, CompressionMode.Decompress))
            {
                _instance.Load(cs);
            }
        }

        public void Save(Stream output)
        {
            using (var cs = new DeflateStream(output, CompressionMode.Compress))
            {
                _instance.Save(cs);
                cs.Flush();
            }
        }
    }
}