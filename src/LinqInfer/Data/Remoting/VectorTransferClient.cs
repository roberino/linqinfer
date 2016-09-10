using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferClient : IDisposable
    {
        private readonly string _clientId;
        private readonly EndPoint _endpoint;
        private readonly Socket _socket;

        public VectorTransferClient(string clientId = null, int port = VectorTransferServer.DefaultPort, string host = "127.0.0.1")
        {
            _clientId = clientId ?? Util.GenerateId();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _endpoint = VectorTransferServer.GetEndpoint(host, port);
        }

        internal VectorTransferClient(string clientId, Socket clientSocket)
        {
            Contract.Assert(clientId != null);
            Contract.Assert(clientSocket != null);

            _clientId = clientId;
            _socket = clientSocket;

            _endpoint = clientSocket.RemoteEndPoint;
        }

        public Task<ITransferHandle> BeginTransfer(string operationType, Action<TransferHandle> onConnect = null)
        {
            var state = new TransferHandle(operationType, _clientId, _socket, onConnect);
            var connectedHandle = new ManualResetEvent(false);

            return Task<ITransferHandle>.Factory.StartNew(() =>
            {
                if (!_socket.Connected)
                {
                    _socket.BeginConnect(_endpoint, a =>
                    {
                        var handle = (TransferHandle)a.AsyncState;

                        handle.ClientSocket.EndConnect(a);
                        handle.OnConnect(handle);
                        connectedHandle.Set();
                    }, state);

                    connectedHandle.WaitOne();
                }
                else
                {
                    connectedHandle.Set();
                }

                return state;
            });
        }

        public void Dispose()
        {
            if (_socket.Connected)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
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
