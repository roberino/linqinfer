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

        private readonly ManualResetEvent _completedHandle;
        private readonly EndPoint _socketEndpoint;

        protected readonly Uri _baseEndpoint;
        protected readonly string _serverId;

        private Socket _socket;

        private volatile ServerStatus _status;
        private volatile bool _transportError;
        private Thread _connectThread;
        private Timer _healthCheck;

        protected ICompressionProvider _compression;

        public HttpServer(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
        {
            _socketEndpoint = GetEndpoint(host, port);

            _baseEndpoint = GetBaseUri(port, host);
            _serverId = serverId ?? Util.GenerateId();
            _completedHandle = new ManualResetEvent(false);

            SetupSocket();

            _status = ServerStatus.Stopped;

            _compression = new DeflateCompressionProvider();

            RequestTimeout = TimeSpan.FromMinutes(2);
        }

        public event EventHandler<EventArgsOf<ServerStatus>> StatusChanged;

        protected virtual Uri GetBaseUri(int port, string host)
        {
            return new Uri(Util.UriSchemeHttp + Util.SchemeDelimiter + host + ":" + port);
        }

        public static EndPoint GetEndpoint(string host, int port = DefaultPort)
        {

#if NET_STD
            var dns = Dns.GetHostEntryAsync(host).Result;
#else
            var dns = Dns.GetHostEntry(host);
#endif
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

        public bool RestoreOnSocketError { get; internal set; }

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
                OnStatusChange(ServerStatus.Error);
                DebugOutput.Log(ex);
                throw new Exception(string.Format("Cant start server on address {0} because of: {1}", _baseEndpoint, ex.Message), ex);
            }

            if (_status == ServerStatus.Running || (_connectThread != null && _connectThread.IsAlive))
            {
                throw new InvalidOperationException("Alread active (status = " + _status + ")");
            }

            SetupConnectThread();

            OnStatusChange(ServerStatus.Running);

            _connectThread.Start();

            SetupHealthCheckThread();

            OnStatusChange(ServerStatus.Running);
        }

        public void Stop(bool wait = false)
        {
            OnStatusChange(ServerStatus.ShuttingDown);

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

        private void SetupSocket()
        {
            _socket = new Socket(_socketEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            _socket.Bind(_socketEndpoint);
        }

        private void SetupHealthCheckThread()
        {
            int failureCount = 0;

            if (_healthCheck != null) _healthCheck.Dispose();

            _transportError = false;

            _healthCheck = new Timer(x =>
            {
                if (
                    _status == ServerStatus.Running ||
                    _status == ServerStatus.Broken)
                {
                    if (_transportError || !_connectThread.IsAlive)
                    {
                        DebugOutput.Log("Health check failure");

                        _transportError = false;

                        OnStatusChange(ServerStatus.Error);

                        bool isErr = false;

                        try
                        {
                            ShutdownSocket();
                            SetupSocket();
                        }
                        catch (Exception ex)
                        {
                            DebugOutput.Log(ex);

                            failureCount++;
                            isErr = true;

                            if (failureCount > 10) throw;
                        }

                        if (!isErr && RestoreOnSocketError)
                        {
                            DebugOutput.Log("Attempting to restart server");

                            OnStatusChange(ServerStatus.Restoring);

                            Start();

                            failureCount = 0;
                        }
                    }
                    else
                    {
                        failureCount = 0;
                    }
                }
            },
            this, TimeSpan.FromMilliseconds(50),TimeSpan.FromMilliseconds(100));            
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
                        //var res = _socket.BeginAccept(AcceptCallback, _socket);
                        
                        var acceptArgs = new SocketAsyncEventArgs();

                        acceptArgs.Completed += AcceptCompleted;

                        if (!_socket.AcceptAsync(acceptArgs))
                        {
                            AcceptCompleted(this, acceptArgs);
                        }

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

                    while (_status == ServerStatus.Running)
                    {
                        if (_completedHandle.WaitOne(500)) break;
                    }
                }

                DebugOutput.Log("Stopping, status = {0}", _status);

                if (_status == ServerStatus.ShuttingDown)
                    OnStatusChange(ServerStatus.Stopped);
            })
            {
                IsBackground = true,                
#if !NET_STD
                Priority = ThreadPriority.Highest
#endif
            };
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            OnAccept(e.AcceptSocket);
        }

        private void OnAccept(Socket handler)
        {
            try
            {
                DebugOutput.Log("Connection accepted from {0}", handler.RemoteEndPoint);

                var processTask = ProcessTcpRequest(handler);

                processTask.Wait(RequestTimeout);
            }
            catch (SocketException ex)
            {
                _transportError = true;

                if (!HandleTransportError(ex))
                {
                    throw;
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    _transportError = true;
                }

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

#if !NET_STD
                        state.ClientSocket.Disconnect(true);
#else
                        state.ClientSocket.Dispose();
#endif
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
            catch (AggregateException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    _transportError = true;
                }

                if (!HandleTransportError(ex))
                {
                    throw;
                }
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

                return new Uri("tcp" + Util.SchemeDelimiter + host + ":" + ip.Port);
            }
            return null;
        }

        private void ShutdownSocket(bool dispose = true)
        {
            DebugOutput.Log("Shutting down socket on port {0}", BaseEndpoint.Port);

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
#if !NET_STD
                _socket.Close(250);
#else
                _socket.Dispose();
#endif
            }
            catch (Exception ex)
            {
                DebugOutput.Log("Error shutting down socket: {0}", ex.Message);
            }

            if (dispose) _socket.Dispose();
        }

        private void OnStatusChange(ServerStatus newStatus)
        {
            if (_status != newStatus)
            {
                _status = newStatus;

                StatusChanged?.Invoke(this, new EventArgsOf<ServerStatus>(newStatus));
            }
        }

        public void Dispose()
        {
            Stop();

            ShutdownSocket(false);

            if (_healthCheck != null)
            {
                _healthCheck.Dispose();
            }

            _completedHandle.Dispose();

            _status = ServerStatus.Disposed;

            _socket.Dispose();

            OnStatusChange(ServerStatus.Disposed);
        }
    }
}