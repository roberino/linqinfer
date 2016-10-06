using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace LinqInfer.Data.Remoting
{
    internal class TcpReceiveContext
    {
        public TcpReceiveContext(Socket client, int bufferSize = 1024)
        {
            ClientSocket = client;
        }

        public TcpRequestHeader Header { get; internal set; }

        public Socket ClientSocket { get; private set; }

        public Stream ReceivedData { get; internal set; }
    }
}