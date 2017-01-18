using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class TransferHandle : ITransferHandle
    {
        private readonly bool _disposeSocket;
        private readonly ICompressionProvider _compression;
        private int _batchIndex;

        public TransferHandle(
            string path, 
            Verb verb,
            string clientId, 
            Socket clientSocket,
            bool disposeSocket,
            ICompressionProvider compression, 
            Action<TransferHandle> onConnect)
        {
            ClientSocket = clientSocket;
            ClientId = clientId;
            Path = path;
            OnConnect = onConnect ?? (h => { });
            Id = Util.GenerateId();
            BufferSize = 1024;
            Verb = verb;

            _compression = compression;
            _disposeSocket = disposeSocket;
        }

        public int BufferSize { get; private set; }

        public string Id { get; private set; }

        public string ClientId { get; private set; }

        public string Path { get; private set; }

        public Verb Verb { get; private set; }

        internal Socket ClientSocket { get; private set; }

        public Action<TransferHandle> OnConnect { get; private set; }

        public Task Send(IEnumerable<ColumnVector1D> data)
        {
            var doc = new BinaryVectorDocument();

            foreach (var d in data) doc.Vectors.Add(d);

            return SendBatch(doc, false);
        }

        public Task Send(BinaryVectorDocument doc)
        {
            return SendBatch(doc, false);
        }

        public async Task<Stream> Receive(object parameters = null)
        {
            var doc = new BinaryVectorDocument();

            foreach (var kv in ParamsToDict(parameters))
            {
                doc.Properties[kv.Key] = kv.Value;
            }

            var results = await SendBatch(doc, false, true);

            return results;
        }

        public Task<Stream> End(object parameters, Uri forwardResponseTo = null)
        {
            return End(ParamsToDict(parameters), forwardResponseTo);
        }

        public async Task<Stream> End(IDictionary<string, string> parameters = null, Uri forwardResponseTo = null)
        {
            var doc = new DataBatch();

            if (parameters != null)
            {
                foreach (var kv in parameters) doc.Properties[kv.Key] = kv.Value;
            }

            if (forwardResponseTo != null)
            {
                doc.ForwardingEndpoint = forwardResponseTo;
            }

            var response = await SendBatch(doc, true);

            DebugOutput.Log("Shutting down client socket");

            ClientSocket.Shutdown(SocketShutdown.Both);

            return response;
        }

        public void Dispose()
        {
            try
            {
                DebugOutput.Log("Disconnecting");
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Disconnect(!_disposeSocket);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                    DebugOutput.Log(ex.Message);
            }
            catch (Exception ex)
            {
                DebugOutput.Log(ex.Message);
            }

            if (_disposeSocket) ClientSocket.Dispose();
        }

        private async Task<Stream> SendBatch(BinaryVectorDocument doc, bool isLast, bool sendResponse = false)
        {
            {
                var transferDoc = new DataBatch();

                foreach (var prop in doc.Properties)
                {
                    transferDoc.Properties[prop.Key] = prop.Value;
                }

                foreach (var vec in doc.Vectors)
                {
                    transferDoc.Vectors.Add(vec);
                }

                foreach (var child in doc.Children)
                {
                    transferDoc.Children.Add(child);
                }

                transferDoc.Id = Id;
                transferDoc.ClientId = ClientId;
                transferDoc.BatchNum = (_batchIndex++);
                transferDoc.KeepAlive = !isLast;
                transferDoc.SendResponse = sendResponse;
                transferDoc.Path = Path;
                transferDoc.Verb = Verb;

                DebugOutput.Log("Sending batch {0}/{1}", Id, transferDoc.BatchNum);

                await Send(this, transferDoc, _compression);
            }

            return await ReceiveData(this, _compression);
        }

        private static async Task Send(TransferHandle state, DataBatch transferDoc, ICompressionProvider compression)
        {
            var buffer = new byte[state.BufferSize];

            using (var ms = new MemoryStream())
            using (var cs = compression.CompressTo(ms))
            {
                transferDoc.Save(cs);

                cs.Flush();
                cs.Dispose();

                ms.Flush();

                ms.Position = 0;

                var sockWriter = new AsyncSocketWriterReader(state.ClientSocket, state.BufferSize);

                var sent = await sockWriter.WriteAsync(ms);

                DebugOutput.LogVerbose("Sent {0} bytes", sent);
            }
        }

        private static async Task<Stream> ReceiveData(TransferHandle state, ICompressionProvider compression)
        {
            var context = await new AsyncSocketWriterReader(state.ClientSocket, state.BufferSize)
                .ReadAsync();

            return compression.DecompressFrom(context.ReceivedData);
        }

        private IDictionary<string, string> ParamsToDict(object parameters)
        {
            if (parameters == null) return new Dictionary<string, string>();

            return parameters
                    .GetType()
                    .GetTypeInf()
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .ToDictionary(p => p.Name, p => p.GetValue(parameters)?.ToString());
        }
    }
}