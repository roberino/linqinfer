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
        private int _batchIndex;

        public TransferHandle(string operationType, string clientId, Socket clientSocket, Action<TransferHandle> onConnect)
        {
            ClientSocket = clientSocket;
            ClientId = clientId;
            OperationType = operationType;
            OnConnect = onConnect ?? (h => { });
            Id = Guid.NewGuid().ToString();
            BufferSize = 1024;
        }

        public int BufferSize { get; private set; }

        public string Id { get; private set; }

        public string ClientId { get; private set; }

        public string OperationType { get; private set; }

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
                if (ClientSocket.Connected)
                {
                    DebugOutput.Log("Disconnecting");
                    ClientSocket.Disconnect(false);
                }
            }
            catch (Exception ex)
            {
                DebugOutput.Log(ex.Message);
            }

            DebugOutput.Log("Disposing");
            ClientSocket.Dispose();
        }

        private async Task<Stream> SendBatch(BinaryVectorDocument doc, bool isLast, bool sendResponse = false)
        {
            await Task.Factory.StartNew(() =>
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
                transferDoc.OperationType = OperationType;

                DebugOutput.Log("Sending batch {0}/{1}", Id, transferDoc.BatchNum);

                Send(this, transferDoc);
            });

            return await Receive(this);
        }

        private static void Send(TransferHandle state, DataBatch transferDoc)
        {
            var buffer = new byte[state.BufferSize];

            using (var ms = new MemoryStream())
            {
                transferDoc.Save(ms);

                ms.Flush();
                ms.Position = 0;

                bool headSent = false;

                int read;
                int sent = 0;

                using (var waitHandle = new ManualResetEvent(false))
                {
                    while (true)
                    {
                        waitHandle.Reset();

                        if (!headSent)
                        {
                            var requestSize = BitConverter.GetBytes(ms.Length);
                            read = requestSize.Length;
                            Array.Copy(requestSize, buffer, read);
                            headSent = true;
                        }
                        else
                        {
                            read = ms.Read(buffer, 0, buffer.Length);
                        }

                        if (read == 0) break;

                        var asyncRes = state.ClientSocket.BeginSend(buffer, 0, read, 0,
                        a =>
                        {
                            var handle = (TransferHandle)a.AsyncState;

                            sent = handle.ClientSocket.EndSend(a);

                            waitHandle.Set();
                        }, state);

                        DebugOutput.Log("Waiting for send ({0} bytes)", read);

                        waitHandle.WaitOne();

                        DebugOutput.Log("Send complete ({0} bytes)", sent);

                        if (sent != read)
                        {
                            throw new ApplicationException("Send failed");
                        }

                        if (sent == 0) break;
                    }
                }
            }
        }

        private static Task<Stream> Receive(TransferHandle state)
        {
            return new AsyncSocketWriterReader(state.ClientSocket, state.BufferSize)
                .ReadAsync();
        }

        private IDictionary<string, string> ParamsToDict(object parameters)
        {
            if (parameters == null) return new Dictionary<string, string>();

            return parameters
                    .GetType()
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .ToDictionary(p => p.Name, p => p.GetValue(parameters)?.ToString());
        }
    }
}