using System.IO;

namespace LinqInfer.Data.Remoting
{
    interface ICompressionProvider
    {
        string Name { get; }
        Stream CompressTo(Stream input, bool closeStream = false);
        Stream DecompressFrom(Stream input);
    }
}