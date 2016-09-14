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
        private MemoryStream _responseStream;

        private int _batchIndex;

        public TransferHandle(string operationType, string clientId, Socket clientSocket, Action<TransferHandle> onConnect)
        {
            ClientSocket = clientSocket;
            ClientId = clientId;
            OperationType = operationType;
            OnConnect = onConnect ?? (h => { });
            Id = Guid.NewGuid().ToString();
            BufferSize = 1024;
            _responseStream = new MemoryStream();
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

            await SendBatch(doc, false, true);

            var results = _responseStream;

            results.Position = 0;

            _responseStream = new MemoryStream();

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

            await SendBatch(doc, true);

            _responseStream.Position = 0;

            return _responseStream;
        }

        private Task SendBatch(BinaryVectorDocument doc, bool isLast, bool sendResponse = false)
        {
            return Task.Factory.StartNew(() =>
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

                var buffer = new byte[BufferSize];

                DebugOutput.Log("Sending batch {0}/{1}", Id, transferDoc.BatchNum);

                using (var ms = new MemoryStream())
                {
                    transferDoc.Save(ms);

                    ms.Flush();
                    ms.Position = 0;

                    using (var waitHandle = new ManualResetEvent(false))
                    {
                        bool headSent = false;

                        while (true)
                        {
                            int read;

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

                            ClientSocket.BeginSend(buffer, 0, read, 0,
                            a =>
                            {
                                var handle = (TransferHandle)a.AsyncState;
                                handle.ClientSocket.EndSend(a);
                                waitHandle.Set();

                            }, this);

                            waitHandle.WaitOne();
                        }
                    }
                }

                Receive(this);
            });
        }

        private static void Receive(TransferHandle state)
        {
            var buffer = new byte[state.BufferSize];

            var pos = 0;

            int received = 0;

            using (var waitHandle = new ManualResetEvent(false))
            {
                int len = 0;

                state.ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, a =>
                {
                    var s = (TransferHandle)a.AsyncState;
                    var headerLen = s.ClientSocket.EndReceive(a);
                    len = headerLen == 0 ? 0 : BitConverter.ToInt32(buffer, 0);
                    waitHandle.Set();
                }, state);

                waitHandle.WaitOne();

                DebugOutput.Log("Receiving response ({0} bytes)", len);

                while (pos < len)
                {
                    waitHandle.Reset();

                    state.ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, a =>
                    {
                        var tx = (TransferHandle)a.AsyncState;

                        received = tx.ClientSocket.EndReceive(a);

                        tx._responseStream.Write(buffer, 0, received);

                        pos += received;

                        waitHandle.Set();
                    }, state);

                    waitHandle.WaitOne();

                    if (received == 0) break;
                }
            }
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