using System.IO;
using System.IO.Compression;

namespace LinqInfer.Data.Remoting
{
    class DeflateCompressionProvider : ICompressionProvider
    {
        public string Name { get { return "deflate"; } }

        public Stream DecompressFrom(Stream input)
        {
            return new DeflateStream(input, CompressionMode.Decompress, true);
        }

        public Stream CompressTo(Stream input, bool closeStream = false)
        {
            return new DeflateStream(input, CompressionMode.Compress, !closeStream);
        }
    }
}