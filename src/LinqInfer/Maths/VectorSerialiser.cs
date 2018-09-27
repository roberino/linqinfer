using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    class VectorSerialiser : IVectorSerialiser
    {
        const string _base64 = "base64";
        const string _csv = "csv";

        public string Serialize(IVector vector, bool useBase64 = true)
        {
            var typeName = vector.GetType().Name;

            string data;

            var method = useBase64 ? _base64 : _csv;

            if (useBase64 || !(vector is ColumnVector1D))
            {
                data = Convert.ToBase64String(vector.ToByteArray());
                method = _base64;
            }
            else
            {
                data = ((ColumnVector1D)vector).ToCsv(int.MaxValue);
            }

            return $"{typeName}/{method};{data}";
        }

        public IVector Deserialize(string data, bool useBase64 = true)
        {
            if (data == null) return null;

            var parts = data.Split(';');

            if (parts.Length == 1)
            {
                if (useBase64)
                {
                    return Vector.FromBase64(data);
                }
                return Vector.FromCsv(data);
            }

            var typeMethod = parts[0].Split('/');
            var typeName = typeMethod[0];

            useBase64 = typeMethod[1] == _base64;

            if (useBase64)
            {
                var factory = FindVectorFactory(typeName);

                return factory.Invoke(Convert.FromBase64String(parts[1]));
            }

            return Vector.FromCsv(parts[1]).ToColumnVector();
        }

        internal static Func<byte[], IVector> FindVectorFactory(string typeName)
        {
            lock (_factoryCache)
            {
                if (!_factoryCache.TryGetValue(typeName, out var factory))
                {
                    var type = typeof(VectorSerialiser)
                                .GetTypeInf()
                                .Assembly
                                .FindTypes<IVector>(t => t.Name == typeName)
                                .FirstOrDefault();

                    _factoryCache[typeName] = factory = type.FindFactory<byte[], IVector>();
                }

                return factory;
            }
        }

        static IDictionary<string, Func<byte[], IVector>> _factoryCache = new Dictionary<string, Func<byte[], IVector>>();
    }
}