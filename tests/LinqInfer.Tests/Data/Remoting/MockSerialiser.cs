using LinqInfer.Data;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    public class MockSerialiser : IObjectSerialiser
    {
        public string MimeType
        {
            get
            {
                return "text/plain";
            }
        }

        public Task Serialise<T>(T obj, Stream output, Encoding encoding)
        {
            LastSerialisedObject = obj;
            var writer = new StreamWriter(output, encoding);
            SerialiseDelegate(obj, writer);
            return Task.FromResult(0);
        }

        public Task<T> Deserialise<T>(Stream input, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public Action<object, TextWriter> SerialiseDelegate { get; set; } = (v, o) => { };

        public object LastSerialisedObject { get; private set; }
    }
}
