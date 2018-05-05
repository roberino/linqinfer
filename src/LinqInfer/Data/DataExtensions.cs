using LinqInfer.Utility;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data
{
    public static class DataExtensions
    {
        public static XmlBlob<T> ToBinaryPersistable<T>(T instance)
            where T : IXmlExportable, IXmlImportable
        {
            return new XmlBlob<T>(instance);
        }

        public static string ToClob(this IBinaryPersistable blob, bool prefixWithTypeName = true)
        {
            using (var ms = new MemoryStream())
            {
                var type = Encoding.UTF8.GetBytes(blob.GetType().AssemblyQualifiedName);             

                using (var deflate = new DeflateStream(ms, CompressionMode.Compress))
                {
                    if (prefixWithTypeName)
                    {
                        var typeLen = BitConverter.GetBytes(type.Length);
                        deflate.Write(typeLen, 0, typeLen.Length);
                        deflate.Write(type, 0, type.Length);
                    }

                    blob.Save(deflate);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static T FromClob<T>(string clob, Func<string, T> instanceFactory) where T : IBinaryPersistable
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(clob)))
            {
                using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    var header = new byte[sizeof(int)];
                    deflate.Read(header, 0, header.Length);
                    var type = new byte[BitConverter.ToInt32(header, 0)];
                    deflate.Read(type, 0, type.Length);

                    var blob = instanceFactory(Encoding.UTF8.GetString(type));

                    blob.Load(deflate);

                    return blob;
                }
            }
        }

        public static T FromClob<T>(this T blob, string clob, bool prefixedWithTypeName = true) where T : IBinaryPersistable
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(clob)))
            {
                using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    if (prefixedWithTypeName)
                    {
                        var header = new byte[sizeof(int)];
                        deflate.Read(header, 0, header.Length);
                        var type = new byte[BitConverter.ToInt32(header, 0)];
                        deflate.Read(type, 0, type.Length);
                    }
                    blob.Load(deflate);
                    return blob;
                }
            }
        }

        public static T FromTypeAnnotatedClob<T>(string clob) where T : IBinaryPersistable
        {
            return FromClob(clob, t =>
            {
                var type = Type.GetType(t);
                var constr = type?.GetTypeInf().GetConstructor(Type.EmptyTypes);
                if (constr == null) throw new ArgumentException("Cant find type or default constructor - " + t + ", " + type);
                return (T)constr.Invoke(new object[0]);
            });
        }

        public static T FromClob<T>(string clob, bool prefixedWithTypeName = true) where T : IBinaryPersistable, new()
        {
            return FromClob(new T(), clob, prefixedWithTypeName);
        }
    }
}