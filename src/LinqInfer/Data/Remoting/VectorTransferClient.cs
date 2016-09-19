using LinqInfer.Utility;
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

        private ICompressionProvider _compression;

        public VectorTransferClient(string clientId = null, int port = VectorTransferServer.DefaultPort, string host = "127.0.0.1")
        {
            _clientId = clientId ?? Util.GenerateId();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _endpoint = VectorTransferServer.GetEndpoint(host, port);
            _compression = new DeflateCompressionProvider();
        }

        internal VectorTransferClient(string clientId, Socket clientSocket)
        {
            Contract.Assert(clientId != null);
            Contract.Assert(clientSocket != null);

            _clientId = clientId;
            _socket = clientSocket;
            _endpoint = clientSocket.RemoteEndPoint;
            _compression = new DeflateCompressionProvider();

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1500);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1500);
        }

        public void CompressUsing(ICompressionProvider compressionProvider)
        {
            Contract.Assert(compressionProvider != null);

            _compression = compressionProvider;
        }

        public Task<ITransferHandle> BeginTransfer(string operationType, Action<TransferHandle> onConnect = null)
        {
            if (_socket.Connected) throw new InvalidOperationException("Already connected");

            var state = new TransferHandle(operationType, _clientId, _socket, _compression, onConnect);
            var connectedHandle = new ManualResetEvent(false);

            return Task<ITransferHandle>.Factory.StartNew(() =>
            {
                if (!_socket.Connected)
                {
                    DebugOutput.Log("Connecting to " + _endpoint);

                    _socket.BeginConnect(_endpoint, a =>
                    {
                        var handle = (TransferHandle)a.AsyncState;

                        handle.ClientSocket.EndConnect(a);
                        handle.OnConnect(handle);
                        connectedHandle.Set();
                    }, state);

                    connectedHandle.WaitOne();

                    if (_socket.Connected) DebugOutput.Log("Connected to " + _endpoint);
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
