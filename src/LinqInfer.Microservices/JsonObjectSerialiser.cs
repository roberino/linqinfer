using LinqInfer.Data.Serialisation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Microservices
{
    public class JsonObjectSerialiser : IObjectSerialiser
    {
        private readonly JsonSerializer _serialiser;

        public JsonObjectSerialiser()
        {
            _serialiser = new JsonSerializer();

            _serialiser.Formatting = Formatting.Indented;
            _serialiser.ContractResolver = new CamelCasePropertyNamesContractResolver();
            _serialiser.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            _serialiser.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
        }

        public string[] SupportedMimeTypes
        {
            get
            {
                return new string[] { "application/json", "text/json" };
            }
        }

        public Task<T> Deserialise<T>(Stream input, Encoding encoding, string mimeType)
        {
            if (!SupportedMimeTypes.Contains(mimeType))
            {
                throw new ArgumentException(mimeType);
            }

            var reader = new StreamReader(input, encoding);
            var obj = _serialiser.Deserialize<T>(new JsonTextReader(reader));
            return Task.FromResult(obj);
        }

        public async Task Serialise<T>(T obj, Encoding encoding, string mimeType, Stream output)
        {
            if (!SupportedMimeTypes.Contains(mimeType))
            {
                throw new ArgumentException(mimeType);
            }

            using (var writer = new StreamWriter(output, encoding, 1024, true))
            {
                _serialiser.Serialize(writer, obj, typeof(T));

                await writer.FlushAsync();
            }
        }
    }
}