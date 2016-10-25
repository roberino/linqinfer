using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public interface IObjectSerialiser
    {
        string[] SupportedMimeTypes { get; }
        Task Serialise<T>(T obj, Encoding encoding, string mimeType, Stream output);
        Task<T> Deserialise<T>(Stream input, Encoding encoding, string mimeType);
    }
}