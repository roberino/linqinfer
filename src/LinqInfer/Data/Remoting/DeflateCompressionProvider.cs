using System.IO;
using System.IO.Compression;

namespace LinqInfer.Data.Remoting
{
    internal class DeflateCompressionProvider : ICompressionProvider
    {
        public Stream DecompressFrom(Stream input)
        {
            return new DeflateStream(input, CompressionMode.Decompress, true);
        }
        public Stream CompressTo(Stream input)
        {
            return new DeflateStream(input, CompressionMode.Compress, true);
        }
    }
}