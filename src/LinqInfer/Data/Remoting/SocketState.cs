using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace LinqInfer.Data.Remoting
{
    internal class SocketState
    {
        public SocketState(Socket client, int bufferSize = 1024)
        {
            ClientSocket = client;
            Buffer = new byte[bufferSize];
            Data = new MemoryStream();
            Header = new Dictionary<string, string>();
        }

        public void Reset()
        {
            ContentLength = null;
            Data.Position = 0;
        }

        public long? ContentLength { get; set; }

        public IDictionary<string, string> Header { get; private set; }

        public Socket ClientSocket { get; private set; }

        public byte[] Buffer { get; private set; }

        public Stream Data { get; private set; }
    }
}