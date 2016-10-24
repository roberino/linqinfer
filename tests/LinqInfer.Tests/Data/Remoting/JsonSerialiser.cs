using LinqInfer.Data;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
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
            try
            {
                var reader = new StreamReader(input, encoding);

                T obj;

                if (typeof(T).IsClass)
                {
                    obj = new JsonSerializer().Deserialize<T>(new JsonTextReader(reader));
                }
                else
                {
                    var str = reader.ReadToEnd();
                    obj = (T)Convert.ChangeType(str, typeof(T));
                }
                return Task.FromResult(obj);
            }
            catch (Exception)
            {
                input.Position = 0;
                var reader = new StreamReader(input, encoding);
                var str = reader.ReadToEnd();

                foreach(var c in str)
                {
                    Console.WriteLine("c = {0} ({1})", c, (int)c);
                }

                Console.WriteLine(str);
                throw;
            }
        }

        public Task Serialise<T>(T obj, Stream output, Encoding encoding)
        {
            using (var writer = new StreamWriter(output, encoding, 1024, true))
            {
                new JsonSerializer().Serialize(writer, obj, typeof(T));
            }
            return Task.FromResult(0);
        }
    }
}
