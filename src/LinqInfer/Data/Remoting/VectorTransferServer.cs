using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferServer : IDisposable, IVectorTransferServer
    {
        public const int DefaultPort = 9012;

        private readonly string _serverId;
        private readonly Socket _socket;
        private readonly ManualResetEvent _completedHandle;
        private IDictionary<string, Func<DataBatch, bool>> _messageHandlers;

        private Thread _connectThread;
        private volatile ServerStatus _status;

        public VectorTransferServer(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
        {
            _serverId = serverId ?? Util.GenerateId();
            _completedHandle = new ManualResetEvent(false);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _socket.Bind(GetEndpoint(host, port));
            _messageHandlers = new Dictionary<string, Func<DataBatch, bool>>();
            _status = ServerStatus.Stopped;
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

        public void AddHandler(string messageType, Func<DataBatch, bool> handler)
        {
            _messageHandlers[messageType] = handler;
        }

        public void Start()
        {
            try
            {
                _socket.Listen(500);
            }
            catch (Exception ex)
            {
                _status = ServerStatus.Error;
                DebugOutput.Log(ex);
                throw;
            }

            if (_status == ServerStatus.Running || (_connectThread != null && _connectThread.IsAlive))
            {
                throw new InvalidOperationException("Alread active (status = " + _status + ")");
            }

            _status = ServerStatus.Running;

            _connectThread = new Thread(() =>
            {
                while (_status == ServerStatus.Running)
                {
                    _completedHandle.Reset();

                    DebugOutput.Log("Waiting for a connection...");

                    _socket.BeginAccept(AcceptCallback, _socket);

                    _completedHandle.WaitOne();
                }

                if (_status == ServerStatus.ShuttingDown)
                    _status = ServerStatus.Stopped;
            });

            _connectThread.Start();
        }

        public void Stop()
        {
            _status = ServerStatus.ShuttingDown;
        }

        public ServerStatus Status { get { return _status; } }

        private void AcceptCallback(IAsyncResult ar)
        {
            _completedHandle.Set();

            var listener = (Socket)ar.AsyncState;

            try
            {
                var handler = listener.EndAccept(ar);

                DebugOutput.Log("Connection accepted from {0}", handler.RemoteEndPoint);

                var state = new SocketState(handler);

                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, ReadCallback, state);
            }
            catch
            {
                if (_status == ServerStatus.Running)
                {
                    _status = ServerStatus.Error;
                    throw;
                }
            }
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

                if (state.Data.Position >= state.ContentLength) // End of transfer ??
                {
                    var more = Process(state);

                    var confirmResponse = BitConverter.GetBytes(state.Data.Position);

                    state.Reset();

                    state.ClientSocket.Send(confirmResponse);

                    //state.ClientSocket.BeginSend(confirmResponse, 0, confirmResponse.Length, 0, a =>
                    //{
                    //    var s = (SocketState)a.AsyncState;

                    //    s.ClientSocket.EndSend(a);
                    //}, state);

                    if (more)
                        state.ClientSocket
                            .BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                                ReadCallback, state);
                }
                else
                {
                    state.ClientSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                       ReadCallback, state);
                }
            }
        }

        private async Task EndRequest(SocketState state, DataBatch endBatch)
        {
            state.ClientSocket.Disconnect(true);

            var fe = endBatch.ForwardingEndpoint;

            if (fe != null)
            {
                using (var client = new VectorTransferClient(_serverId, fe.Port, fe.Host))
                {
                    var tx = await client.BeginTransfer(fe.PathAndQuery);

                    await tx.Send(endBatch);
                    await tx.End();
                }
            }
        }

        private bool Process(SocketState state)
        {
            try
            {
                state.Data.Position = 0;

                var doc = new DataBatch();

                doc.Load(state.Data);

                DebugOutput.Log("Processing batch {0}/{1}", doc.Id, doc.BatchNum);

                _messageHandlers[doc.OperationType](doc);

                if (!doc.KeepAlive)
                {
                    var end = EndRequest(state, doc);

                    end.Wait();

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                DebugOutput.Log(ex);
                return false;
            }
        }

        public void Dispose()
        {
            Stop();

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

            _completedHandle.Dispose();

            _socket.Dispose();
        }
    }
}
