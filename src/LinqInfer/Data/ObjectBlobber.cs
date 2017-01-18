using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Data
{
    internal class ObjectBlobber<T> : IBinaryPersistable
    {
        private readonly IObjectSerialiser _serialiser;

        public ObjectBlobber(IObjectSerialiser serialiser, T instance)
        {
            _serialiser = serialiser;
            Instance = instance;
        }

        public T Instance { get; private set; }

        public void Load(Stream input)
        {
            Instance = _serialiser.Deserialise<T>(input, Encoding.UTF8, _serialiser.SupportedMimeTypes.First()).Result;
        }

        public void Save(Stream output)
        {
            _serialiser.Serialise(Instance, Encoding.UTF8, _serialiser.SupportedMimeTypes.First(), output);
        }
    }
}