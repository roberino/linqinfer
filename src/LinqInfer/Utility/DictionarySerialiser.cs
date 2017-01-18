using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Utility
{
    public static class DictionarySerialiserFactory
    {
        public static DictionarySerialiser<TKey, TValue> ForInstance<TKey, TValue>(IDictionary<TKey, TValue> data, Encoding encoding = null)
        {
            return new DictionarySerialiser<TKey, TValue>(encoding);
        }
    }

    public class DictionarySerialiser<TKey, TValue>
    {
        protected readonly Encoding _encoding;

        public DictionarySerialiser(Encoding encoding = null)
        {
            _encoding = encoding ?? Encoding.UTF8;
        }

        public void Write(IDictionary<TKey, TValue> data, Stream output)
        {
            using (var writer = new BinaryWriter(output, _encoding, true))
            {
                writer.Write(data.Count);
                writer.Write(data.GetType().FullName);

                foreach (var kv in data)
                {
                    Write(writer, kv.Key);
                    Write(writer, kv.Value);
                }

                writer.Flush();
            }
        }

        public IDictionary<TKey, TValue> Read(Stream input)
        {
            using(var reader = new BinaryReader(input, _encoding, true))
            {
                var count = reader.ReadInt32();
                var typeName = reader.ReadString();
                var data = CreateDictionary(typeName);

                foreach (var n in Enumerable.Range(0, count))
                {
                    data[Read<TKey>(reader)] = Read<TValue>(reader);
                }

                return data;
            }
        }

        protected virtual IDictionary<TKey, TValue> CreateDictionary(string typeName)
        {
            var type = Type.GetType(typeName, false, false);

            if (type != null)
            {
                try
                {
                    //var ctype = type.MakeGenericType(typeof(TKey), typeof(TValue));

                    return (IDictionary<TKey, TValue>)type.GetTypeInf().GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                }
                catch (Exception ex)
                {
                    DebugOutput.Log(ex);
                }
            }
            return new Dictionary<TKey, TValue>();
        }

        protected virtual T Read<T>(BinaryReader reader)
        {
            var type = typeof(T);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                    return (T)(object)reader.ReadInt16();
                case TypeCode.Int32:
                    return (T)(object)reader.ReadInt32();
                case TypeCode.Int64:
                    return (T)(object)reader.ReadInt64();
                case TypeCode.Single:
                    return (T)(object)reader.ReadSingle();
                case TypeCode.Double:
                    return (T)(object)reader.ReadDouble();
                case TypeCode.Byte:
                    return (T)(object)reader.ReadByte();
                case TypeCode.Boolean:
                    return (T)(object)reader.ReadBoolean();
                case TypeCode.Char:
                    return (T)(object)reader.ReadChar();
                case TypeCode.String:
                    return (T)(object)reader.ReadString();
                case TypeCode.DateTime:
                    return (T)(object)DateTime.FromFileTimeUtc(reader.ReadInt64());
                case TypeCode.Object:

                    if (typeof(IBinaryPersistable).GetTypeInf().IsAssignableFrom(type))
                    {
                        var typeName = reader.ReadString();
                        var actualType = Type.GetType(typeName) ?? typeof(T);
                        var instance = (IBinaryPersistable)actualType.GetTypeInf().GetConstructor(Type.EmptyTypes).Invoke(new object[0]);

                        instance.Load(reader.BaseStream);

                        return (T)instance;
                    }

                    break;
            }

            throw new NotSupportedException(type.FullName);
        }

        protected virtual void Write<T>(BinaryWriter writer, T obj)
        {
            var type = typeof(T);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                    writer.Write((short)(object)obj);
                    break;
                case TypeCode.Int32:
                    writer.Write((int)(object)obj);
                    break;
                case TypeCode.Int64:
                    writer.Write((long)(object)obj);
                    break;
                case TypeCode.DateTime:
                    writer.Write(((DateTime)(object)obj).ToFileTimeUtc());
                    break;
                case TypeCode.Byte:
                    writer.Write((byte)(object)obj);
                    break;
                case TypeCode.Single:
                    writer.Write((float)(object)obj);
                    break;
                case TypeCode.Double:
                    writer.Write((double)(object)obj);
                    break;
                case TypeCode.Char:
                    writer.Write((char)(object)obj);
                    break;
                case TypeCode.Boolean:
                    writer.Write((bool)(object)obj);
                    break;
                case TypeCode.String:
                    writer.Write((string)(object)obj);
                    break;
                case TypeCode.Object:
                    if (typeof(IBinaryPersistable).GetTypeInf().IsAssignableFrom(type))
                    {
                        if (obj != null)
                        {
                            var actualType = obj.GetType();

                            if(!actualType.GetTypeInf().GetConstructors().Any(c => !c.GetParameters().Any()))
                            {
                                throw new NotSupportedException(actualType.FullName + " missing default constructor");
                            }

                            writer.Write(actualType.AssemblyQualifiedName);
                            ((IBinaryPersistable)obj).Save(writer.BaseStream);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(type.FullName);
                    }
                    break;
                default:
                    throw new NotSupportedException(type.FullName);
            }
        }
    }
}
