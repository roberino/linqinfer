using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public sealed class TcpRequest : IBinaryPersistable, ICloneableObject<TcpRequest>
    {
        internal TcpRequest(IRequestHeader header, Stream body)
        {
            Header = header;
            Content = body;
        }

        public IRequestHeader Header { get; private set; }

        public Stream Content { get; private set; }

        public async Task<string> ToStringAsync()
        {
            if (Content is MemoryStream)
            {
                var buffer = ((MemoryStream)Content).ToArray();

                //Header.HeaderLength;

                return Header.ContentEncoding.GetString(((MemoryStream)Content).ToArray());
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    await Content.CopyToAsync(ms);

                    if (!Content.CanSeek)
                    {
                        ms.Position = 0;
                        Content = ms;
                    }
                    else
                    {
                        Content.Position = 0;
                    }

                    return Header.ContentEncoding.GetString(ms.ToArray());
                }
            }
        }

        public void Save(Stream output)
        {
            if (Header.TransportProtocol == TransportProtocol.Http)
            {
                using (var writer = new StreamWriter(output, Encoding.ASCII, 1024, true))
                {
                    using (var formatter = new HttpHeaderFormatter(writer))
                    {
                        formatter.WriteRequestAndProtocol(Header.HttpVerb, Header.Path, Header.HttpProtocol);
                        formatter.WriteHeaders(Header.Headers);
                        formatter.WriteEnd();
                    }
                }
            }
            else
            {
                new BinaryWriter(output, Encoding.UTF8, true).Write(Header.ContentLength);
            }

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