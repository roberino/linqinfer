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

        private readonly RoutingTable<IOwinContext> _routes;

        public VectorTransferServer(string serverId = null, int port = DefaultTcpPort, string host = "127.0.0.1")
            : base(serverId, port, host)
        {
            _routes = new RoutingTable<IOwinContext>();
        }

        public void AddHandler(IUriRoute route, Func<DataBatch, TcpResponse, bool> handler)
        {
            AddAsyncHandler(route, (b, r) => Task.FromResult(handler(b, r)));
        }

        public void AddAsyncHandler(IUriRoute route, Func<DataBatch, TcpResponse, Task<bool>> handler)
        {
            _routes.AddHandler(route, (p, c) =>
            {
                var doc = c["ext.DataBatch"] as DataBatch;

                foreach (var parameter in p)
                {
                    doc.Properties[parameter.Key.ToLower()] = parameter.Value;
                }

                return handler(doc, c.Response);
            });
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

        private DataBatch ParseRequest(IOwinContext context)
        {
            var doc = new DataBatch();

            {
                if (context.Request.Header.ContentLength > 0)
                {
                    doc.Load(context.Request.Content);

                    ((OwinContext)context).Path = doc.Path;
                }
                else
                {
                    doc.Id = Util.GenerateId();
                    doc.BatchNum = 1;
                    doc.Path = context.Request.Header.Path;
                    doc.Verb = context.Request.Header.Verb;
                }
            }

            return doc;
        }

        protected override async Task<bool> Process(IOwinContext context)
        {
            var doc = ParseRequest(context);

            DebugOutput.Log("Processing batch {0}/{1}", doc.Id, doc.BatchNum);

            context["ext.DataBatch"] = doc;

            var handler = _routes.Map(new Uri(_baseEndpoint, context.Request.Header.Path), doc.Verb);

            if (handler == null)
            {
                DebugOutput.Log("Missing handler for route: {0}/{1}", doc.Path, doc.Verb);

                return false;
            }

            using (var mutex = new Mutex(true, doc.Id))
            {
                if (!mutex.WaitOne())
                {
                    throw new InvalidOperationException("Cant aquire lock on " + doc.Id);
                }
                var res = await handler.Invoke(context);
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