using LinqInfer.Maths;
using System;
using System.Net;
using System.Net.Sockets;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferServer : IDisposable
    {
        private readonly Socket _socket;

        public VectorTransferServer(int port = 9012, string host = "127.0.0.1")
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            _socket.Bind(new DnsEndPoint(host, 9012));
        }

        public Func<bool, ColumnVector1D> OnRecieve { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            _socket.Listen(1000);
        }
    }
}
