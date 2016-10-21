using LinqInfer.Data;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    public class JsonSerialiser : IObjectSerialiser
    {
        public string MimeType
        {
            get
            {
                return "application/json";
            }
        }

        public Task<T> Deserialise<T>(Stream input, Encoding encoding)
        {
            var reader = new StreamReader(input, encoding);
            var obj = new JsonSerializer().Deserialize<T>(new JsonTextReader(reader));
            return Task.FromResult(obj);
        }

        public Task Serialise<T>(T obj, Stream output, Encoding encoding)
        {
            using (var writer = new StreamWriter(output, encoding, 1024, true))
            {
                new JsonSerializer().Serialize(writer, obj);
            }
            return Task.FromResult(0);
        }
    }
}
