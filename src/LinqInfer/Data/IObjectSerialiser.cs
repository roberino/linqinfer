using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public interface IObjectSerialiser
    {
        string MimeType { get; }
        Task Serialise<T>(T obj, Stream output, Encoding encoding);
        Task<T> Deserialise<T>(Stream input, Encoding encoding);
    }
}