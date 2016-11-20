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
    internal class HttpServer : IServer
    {
        public const int DefaultPort = 80;

        private readonly Socket _socket;
        private readonly ManualResetEvent _completedHandle;

        protected readonly Uri _baseEndpoint;
        protected readonly string _serverId;

        private volatile ServerStatus _status;
        private volatile bool _transportError;
        private Thread _connectThread;
        private Thread _healthCheckThread;

        protected ICompressionProvider _compression;

        public HttpServer(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
        {
            var endpoint = GetEndpoint(host, port);

            _baseEndpoint = GetBaseUri(port, host);
            _serverId = serverId ?? Util.GenerateId();
            _completedHandle = new ManualResetEvent(false);
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            _socket.Bind(endpoint);
            _status = ServerStatus.Stopped;

            _compression = new DeflateCompressionProvider();

            RequestTimeout = TimeSpan.FromMinutes(2);
        }

        protected virtual Uri GetBaseUri(int port, string host)
        {
            return new Uri(Uri.UriSchemeHttp + Uri.SchemeDelimiter + host + ":" + port);
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

        public TimeSpan RequestTimeout { get; set; }

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

        public void Start()
        {
            _transportError = false;

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

            SetupConnectThread();

            _connectThread.Start();

            _status = ServerStatus.Running;
        }

        public void Stop(bool wait = false)
        {
            _status = ServerStatus.ShuttingDown;

            if (wait)
            {
                if (_connectThread.IsAlive)
                {
                    _connectThread.Join(1500);
                }
            }
        }

        public ServerStatus Status
        {
            get
            {
                if (_status == ServerStatus.Running)
                {
                    if (!_connectThread.IsAlive)
                    {
                        return ServerStatus.Broken;
                    }
                }

                return _status;
            }
        }

        protected virtual Task<bool> Process(IOwinContext context)
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> HandleRequestError(Exception error, TcpResponse response)
        {
            DebugOutput.Log(error);

            return Task.FromResult(true);
        }

        protected virtual bool HandleTransportError(Exception ex)
        {
            DebugOutput.Log("Transport error: ", ex.Message);

            return false;
        }

        private void SetupConnectThread()
        {
            var consecErr = 0;

            _connectThread = new Thread(() =>
            {
                while (_status == ServerStatus.Running)
                {
                    _completedHandle.Reset();

                    try
                    {
                        var res = _socket.BeginAccept(AcceptCallback, _socket);

                        DebugOutput.Log("Waiting for a connection on port {0}...", _baseEndpoint.Port);

                        consecErr = 0;
                    }
                    catch (Exception ex)
                    {
                        consecErr++;
                        DebugOutput.Log("Error accepting - ", ex);
                        _completedHandle.Set();

                        if (consecErr > 5)
                        {
                            Thread.Sleep(50);
                        }
                    }

                    // TODO: try _socket.AcceptAsync()

                    //var acceptArgs = new SocketAsyncEventArgs();

                    //acceptArgs.Completed += AcceptCompleted;

                    //if (!_socket.AcceptAsync(acceptArgs))
                    //{
                    //    AcceptCompleted(this, acceptArgs);
                    //}

                    while (_status == ServerStatus.Running)
                    {
                        if (_completedHandle.WaitOne(500)) break;
                    }
                }

                DebugOutput.Log("Stopping, status = {0}", _status);

                if (_status == ServerStatus.ShuttingDown)
                    _status = ServerStatus.Stopped;
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            OnAccept(e.AcceptSocket);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            _completedHandle.Set();

            var listener = (Socket)ar.AsyncState;

            try
            {
                var handler = listener.EndAccept(ar);

                OnAccept(handler);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException) return;

                if (!HandleTransportError(ex))
                {
                    throw;
                }
            }
        }

        private void OnAccept(Socket handler)
        {
            try
            {
                DebugOutput.Log("Connection accepted from {0}", handler.RemoteEndPoint);

                var processTask = ProcessTcpRequest(handler);

                processTask.Wait(RequestTimeout);

                // handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, ReadCallback, state);
            }
            catch (SocketException ex)
            {
                _transportError = true;

                if (!HandleTransportError(ex))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (!HandleTransportError(ex))
                {
                    throw;
                }
            }
        }

        private async Task ProcessTcpRequest(Socket clientSocket)
        {
            bool more = true;

            while (more)
            {
                var reader = new AsyncSocketWriterReader(clientSocket);

                using (var state = await reader.ReadAsync())
                {
                    using (var tcpResponse = new TcpResponse(state.Header, _compression))
                    {
                        more = await ExecuteRequest(state, tcpResponse);
                    }

                    if (more)
                    {
                        DebugOutput.LogVerbose("Waiting for more data");
                    }
                    else
                    {
                        DebugOutput.Log("Ending conversation");
                        state.ClientSocket.Disconnect(true);
                    }
                }
            }
        }

        private async Task<bool> ExecuteRequest(TcpReceiveContext state, TcpResponse tcpResponse)
        {
            bool continueReceive = false;

            try
            {
                continueReceive = await Process(state, tcpResponse);
            }
            catch (Exception ex)
            {
                if (!await HandleRequestError(ex, tcpResponse))
                {
                    throw;
                }
            }

            await SendResponse(state, tcpResponse);
            
            return continueReceive;
        }

        private async Task SendResponse(TcpReceiveContext state, TcpResponse tcpResponse)
        {
            var content = tcpResponse.GetSendStream();

            DebugOutput.Log("Sending response ({0} bytes)", content.Length);

            var sockStream = new AsyncSocketWriterReader(state.ClientSocket);

            await sockStream.WriteAsync(content, tcpResponse.Header);
        }

        private async Task<bool> Process(TcpReceiveContext state, TcpResponse response)
        {
            Stream requestBody;

            if (state.Header.ContentLength > 0)
            {
                state.ReceivedData.Position = 0;

                requestBody = _compression.DecompressFrom(state.ReceivedData);
            }
            else
            {
                requestBody = Stream.Null;
            }

            var context = new OwinContext(new TcpRequest(state.Header, requestBody), response, GetUriEndpoint(state.ClientSocket.RemoteEndPoint));

            state.Cleanup.Add(response);
            state.Cleanup.Add(requestBody);

            return await Process(context);
        }

        private Uri GetUriEndpoint(EndPoint endpoint)
        {
            if (endpoint is IPEndPoint)
            {
                var ip = ((IPEndPoint)endpoint);

                string host;

                try
                {
                    var address = ip.Address.MapToIPv4();

                    host = address.ToString();
                }
                catch
                {
                    host = "localhost";
                }

                return new Uri("tcp" + Uri.SchemeDelimiter + host + ":" + ip.Port);
            }
            return null;
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