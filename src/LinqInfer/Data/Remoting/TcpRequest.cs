using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public sealed class TcpRequest : IBinaryPersistable, ICloneableObject<TcpRequest>
    {
        internal TcpRequest(TcpRequestHeader header, Stream body)
        {
            Header = header;
            Content = body;
        }

        public TcpRequestHeader Header { get; private set; }

        public Stream Content { get; private set; }

        public void Save(Stream output)
        {
            Header.WriteTo(output);

            if (Content.CanRead && Content.Position > 0)
                Content.CopyTo(output);
        }

        public void Load(Stream input)
        {
            var buffer = new byte[4096];

            TcpRequestHeader header = null;
            var content = new MemoryStream();

            while (true)
            {
                var read = input.Read(buffer, 0, buffer.Length);

                if (read == 0) break;

                if (header == null)
                {
                    header = new TcpRequestHeader(buffer);

                    if (header.HeaderLength < read)
                    {
                        content.Write(buffer, header.HeaderLength, read - header.HeaderLength);
                    }
                }
                else
                {
                    content.Write(buffer, 0, read);
                }
            }

            if (header == null)
            {
                throw new ArgumentException("Invalid header");
            }

            Header = header;
            Content = content;
        }

        public TcpRequest Clone(bool deep)
        {
            if (deep)
            {
                if (Content == Stream.Null)
                {
                    var ms = new MemoryStream();
                    Content.CopyTo(ms);
                    return new TcpRequest(Header, ms);
                }
            }

            return new TcpRequest(Header, Content);
        }
    }
}