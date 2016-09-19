using System.IO;

namespace LinqInfer.Data.Remoting
{
    internal interface ICompressionProvider
    {
        Stream CompressTo(Stream input);
        Stream DecompressFrom(Stream input);
    }
}