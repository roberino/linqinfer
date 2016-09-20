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
            _endpoint = VectorTransferServer.GetEndpoint(host, port);
            _compression = new DeflateCompressionProvider();
        }

        internal VectorTransferClient(string clientId, Socket clientSocket)
        {
            Contract.Assert(clientId != null);
            Contract.Assert(clientSocket != null);

            _clientId = clientId;
            _socket = SetupSocket(clientSocket);
            _endpoint = clientSocket.RemoteEndPoint;
            _compression = new DeflateCompressionProvider();
        }

        public int Timeout { get; set; } = 1500;

        private Socket CreateSocket()
        {
            return SetupSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
        }

        private Socket SetupSocket(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout);

            return socket;
        }

        public void CompressUsing(ICompressionProvider compressionProvider)
        {
            Contract.Assert(compressionProvider != null);

            _compression = compressionProvider;
        }

        public Task<ITransferHandle> BeginTransfer(string path, Verb verb = Verb.Default, Action<TransferHandle> onConnect = null)
        {
            var socket = _socket ?? CreateSocket();

            if (socket.Connected) throw new InvalidOperationException("Already connected");

            var state = new TransferHandle(path, verb, _clientId, socket, _socket == null, _compression, onConnect);
            var connectedHandle = new ManualResetEvent(false);

            return Task<ITransferHandle>.Factory.StartNew(() =>
            {
                if (!socket.Connected)
                {
                    DebugOutput.Log("Connecting to " + _endpoint);

                    socket.BeginConnect(_endpoint, a =>
                    {
                        var handle = (TransferHandle)a.AsyncState;

                        handle.ClientSocket.EndConnect(a);
                        handle.OnConnect(handle);
                        connectedHandle.Set();
                    }, state);

                    connectedHandle.WaitOne();

                    if (socket.Connected) DebugOutput.Log("Connected to " + _endpoint);
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
            if (_socket != null)
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
}
