using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferServer : IDisposable
    {
        public const int DefaultPort = 9012;

        private readonly Socket _socket;
        private readonly ManualResetEvent _completedHandle;
        private IDictionary<string, Func<BinaryVectorDocument, bool>> _messageHandlers;

        private Thread _connectThread;
        private bool _stop;

        public VectorTransferServer(int port = DefaultPort, string host = "127.0.0.1")
        {
            _completedHandle = new ManualResetEvent(false);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _socket.Bind(GetEndpoint(host, port));
            _messageHandlers = new Dictionary<string, Func<BinaryVectorDocument, bool>>();
        }

        public static EndPoint GetEndpoint(string host, int port = DefaultPort)
        {
            var dns = Dns.GetHostEntry(host);
            var ipAddress = dns.AddressList[0];
            return new IPEndPoint(ipAddress, port);
        }

        public static EndPoint GetDefaultEndpoint(int port = DefaultPort)
        {
            var ipAddress = IPAddress.Loopback;
            return new IPEndPoint(ipAddress, port);
        }

        public void AddHandler(string messageType, Func<BinaryVectorDocument, bool> handler)
        {
            _messageHandlers[messageType] = handler;
        }

        public void Start()
        {
            _socket.Listen(500);

            if (_connectThread != null && _connectThread.IsAlive)
            {
                throw new InvalidOperationException("Alread running");
            }

            _stop = false;

            _connectThread = new Thread(() =>
            {
                while (!_stop)
                {
                    _completedHandle.Reset();

                    DebugOutput.Log("Waiting for a connection...");

                    _socket.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        _socket);

                    _completedHandle.WaitOne();
                }
            });

            _connectThread.Start();
        }

        public void Stop()
        {
            _stop = true;
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            _completedHandle.Set();

            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            var state = new SocketState(handler);

            handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var state = (SocketState)ar.AsyncState;

            int bytesRead = state.ClientSocket.EndReceive(ar);

            if (bytesRead > 0)
            {
                if (!state.ContentLength.HasValue)
                {
                    state.ContentLength = BitConverter.ToInt64(state.Buffer, 0);
                }
                else
                {
                    state.Data.Write(state.Buffer, 0, bytesRead);
                }

                if (state.Data.Length >= state.ContentLength) // End of transfer ??
                {
                    Process(state);

                    return;
                }
                else
                {
                    state.ClientSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                       new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private void Process(SocketState state)
        {
            state.Data.Position = 0;

            var doc = new BinaryVectorDocument();

            doc.Load(state.Data);

            var opType = doc.Properties["OpType"];

            _messageHandlers[opType](doc);
        }

        public void Dispose()
        {
            Stop();

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
