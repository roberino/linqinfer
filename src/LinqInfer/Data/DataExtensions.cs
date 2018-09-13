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
    }
}