using LinqInfer.Utility;
using System;
using System.Diagnostics.Contracts;
using System.IO;
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
        private readonly RoutingTable _routes;
        private readonly Uri _baseEndpoint;

        private Thread _connectThread;
        private volatile ServerStatus _status;

        private ICompressionProvider _compression;

        public VectorTransferServer(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
        {
            var endpoint = GetEndpoint(host, port);

            _baseEndpoint = new Uri("tcp://" + host + ":" + port);
            _serverId = serverId ?? Util.GenerateId();
            _completedHandle = new ManualResetEvent(false);
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            _socket.Bind(endpoint);
            _routes = new RoutingTable();
            _status = ServerStatus.Stopped;

            _compression = new DeflateCompressionProvider();            
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

        public Uri BaseEndpoint
        {
            get
            {
                return _baseEndpoint;
            }
        }

        public void CompressUsing(ICompressionProvider compressionProvider)
        {
            Contract.Assert(compressionProvider != null);

            _compression = compressionProvider;
        }

        public void AddHandler(UriRoute route, Func<DataBatch, TcpResponse, bool> handler)
        {
            _routes.AddHandler(route, handler);
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
                throw new ApplicationException(string.Format("Cant start server on address {0} because of: {1}", _baseEndpoint, ex.Message), ex);
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

                var state = new TcpRequestContext(handler);

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
            var state = (TcpRequestContext)ar.AsyncState;

            int bytesRead = state.ClientSocket.EndReceive(ar);

            if (bytesRead > 0)
            {
                if (!state.ContentLength.HasValue)
                {
                    var header = new TcpRequestHeader(state.Buffer);
                    state.Header = header;
                    state.ContentLength = header.ContentLength;
                }
                else
                {
                    state.ReceivedData.Write(state.Buffer, 0, bytesRead);
                }

                if (state.ReceivedData.Position >= state.ContentLength)
                {
                    bool more = false;

                    using (var tcpResponse = new TcpResponse(_compression))
                    {
                        more = Process(state, tcpResponse);

                        SendResponse(state, tcpResponse);

                        state.Reset();
                    }

                    if (more)
                    {
                        state.ClientSocket
                            .BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                                ReadCallback, state);
                    }
                    else
                    {
                        DebugOutput.Log("Ending conversation");
                        state.ClientSocket.Disconnect(true);
                    }
                }
                else
                {
                    state.ClientSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0,
                       ReadCallback, state);
                }
            }
        }

        private void SendResponse(TcpRequestContext state, TcpResponse response)
        {
            var content = response.GetSendStream();

            DebugOutput.Log("Sending response ({0} bytes)", content.Length);

            response.Header.TransportProtocol = state.Header.TransportProtocol;

            var sockStream = new AsyncSocketWriterReader(state.ClientSocket);

            sockStream.Write(content, response.Header);
        }

        private async Task EndRequest(TcpRequestContext state, DataBatch endBatch)
        {
            var fe = endBatch.ForwardingEndpoint;

            if (fe != null)
            {
                DebugOutput.Log("Forwarding to {0}", fe);

                using (var client = new VectorTransferClient(_serverId, fe.Port, fe.Host))
                {
                    client.CompressUsing(_compression);

                    var tx = await client.BeginTransfer(fe.PathAndQuery);

                    await tx.Send(endBatch);
                    await tx.End(endBatch.Properties);
                }
            }
        }

        private bool Process(TcpRequestContext state, TcpResponse response)
        {
            try
            {
                var doc = new DataBatch();

                {
                    if (state.Header.ContentLength > 0)
                    {
                        state.ReceivedData.Position = 0;

                        var cs = _compression.DecompressFrom(state.ReceivedData);

                        doc.Load(cs);
                    }
                    else
                    {
                        doc.Id = Util.GenerateId();
                        doc.BatchNum = 1;
                        doc.Path = state.Header.Path;
                        doc.Verb = state.Header.Verb;
                    }
                }

                DebugOutput.Log("Processing batch {0}/{1}", doc.Id, doc.BatchNum);

                var handler = _routes.Map(new Uri(_baseEndpoint, doc.Path), doc.Verb);

                if (handler == null)
                {
                    DebugOutput.Log("Missing handler for route: {0}/{1}", doc.Path, doc.Verb);

                    return false;
                }

                handler.Invoke(doc, response);

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

                Write(response.Content, d =>
                {
                    d.Properties["ErrorMessage"] = ex.Message;
                    d.Properties["ErrorType"] = ex.GetType().AssemblyQualifiedName;
                    d.Properties["ErrorStackTrace"] = ex.StackTrace;
                });

                return false;
            }
        }

        private void Write(Stream response, Action<BinaryVectorDocument> writer)
        {
            var doc = new BinaryVectorDocument();

            writer(doc);

            doc.Save(response);
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

            _status = ServerStatus.Disposed;

            _socket.Dispose();
        }
    }
}