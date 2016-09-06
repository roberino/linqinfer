using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferClient : IDisposable
    {
        private readonly EndPoint _endpoint;
        private readonly Socket _socket;

        public VectorTransferClient(int port = VectorTransferServer.DefaultPort, string host = "127.0.0.1")
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _endpoint = VectorTransferServer.GetEndpoint(host, port);
        }

        public Task<TransferHandle> BeginTransfer(string operationType, Action<TransferHandle> onConnect = null)
        {
            var state = new TransferHandle(operationType, new SocketState(_socket), onConnect);
            var connectedHandle = new ManualResetEvent(false);

            return Task<TransferHandle>.Factory.StartNew(() =>
            {
                _socket.BeginConnect(_endpoint, a =>
                {
                    var handle = (TransferHandle)a.AsyncState;

                    handle.State.ClientSocket.EndConnect(a);
                    handle.OnConnect(handle);
                    connectedHandle.Set();
                }, state);

                connectedHandle.WaitOne();

                return state;
            });
        }

        public void Dispose()
        {
            if (_socket.Connected)
            {
                try
                {
                    _socket.Close();
                }
                catch
                {

                }
            }

            _socket.Dispose();
        }
    }
}
