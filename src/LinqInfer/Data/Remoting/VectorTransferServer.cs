using LinqInfer.Utility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class VectorTransferServer : HttpServer, IVectorTransferServer
    {
        public const int DefaultTcpPort = 9012;

        private readonly RoutingTable _routes;

        public VectorTransferServer(string serverId = null, int port = DefaultTcpPort, string host = "127.0.0.1")
            : base(serverId, port, host)
        {
            _routes = new RoutingTable();
        }

        public void AddHandler(UriRoute route, Func<DataBatch, TcpResponse, bool> handler)
        {
            _routes.AddHandler(route, (b, r) => Task.FromResult(handler(b, r)));
        }

        public void AddAsyncHandler(UriRoute route, Func<DataBatch, TcpResponse, Task<bool>> handler)
        {
            _routes.AddHandler(route, handler);
        }

        private async Task EndRequest(DataBatch endBatch)
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

        protected override async Task<bool> Process(IOwinContext context)
        {
            var doc = new DataBatch();

            {
                if (context.RequestHeader.ContentLength > 0)
                {
                    doc.Load(context.RequestBody);
                }
                else
                {
                    doc.Id = Util.GenerateId();
                    doc.BatchNum = 1;
                    doc.Path = context.RequestHeader.Path;
                    doc.Verb = context.RequestHeader.Verb;
                }
            }

            DebugOutput.Log("Processing batch {0}/{1}", doc.Id, doc.BatchNum);

            var handler = _routes.Map(new Uri(_baseEndpoint, doc.Path), doc.Verb);

            if (handler == null)
            {
                DebugOutput.Log("Missing handler for route: {0}/{1}", doc.Path, doc.Verb);

                return false;
            }

            DebugOutput.Log("Invoking handler");

            using (var mutex = new Mutex(true, doc.Id))
            {
                try
                {
                    if (!mutex.WaitOne())
                    {
                        throw new InvalidOperationException("Cant aquire lock on " + doc.Id);
                    }
                    var res = await handler.Invoke(doc, context.Response);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            if (!doc.KeepAlive)
            {
                DebugOutput.Log("Ending request");

                await EndRequest(doc);

                return false;
            }

            return true;
        }

        protected override Task<bool> HandleRequestError(Exception error, TcpResponse response)
        {
            DebugOutput.Log(error);

            Write(response.Content, d =>
            {
                d.Properties["ErrorMessage"] = error.Message;
                d.Properties["ErrorType"] = error.GetType().AssemblyQualifiedName;
                d.Properties["ErrorStackTrace"] = error.StackTrace;
            });

            return Task.FromResult(true);
        }

        private void Write(Stream response, Action<BinaryVectorDocument> writer)
        {
            var doc = new BinaryVectorDocument();

            writer(doc);

            doc.Save(response);
        }
    }
}