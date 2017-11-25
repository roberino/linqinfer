using LinqInfer.Utility;
using System;
using System.Linq;

namespace LinqInfer.Maths
{
    internal class VectorSerialiser : IVectorSerialiser
    {
        private const string _base64 = "base64";
        private const string _csv = "csv";

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
            else
            {
                var typeMethod = parts[0].Split('/');
                var typeName = typeMethod[0];

                useBase64 = typeMethod[1] == _base64;

                if (useBase64)
                {
                    var types = GetType().GetTypeInf().Assembly.FindTypes<IVector>(t => t.Name == typeName);

                    var type = types.FirstOrDefault();

                    var factory = type.FindFactory<byte[], IVector>();

                    return factory.Invoke(Convert.FromBase64String(parts[1]));
                }
                else
                {
                    return Vector.FromCsv(parts[1]);
                }
            }
        }
    }
}