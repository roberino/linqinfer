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

        public Task End(Uri forwardResponseTo = null)
        {
            var doc = new DataBatch();

            if (forwardResponseTo != null)
            {
                doc.ForwardingEndpoint = forwardResponseTo;
            }

            return SendBatch(doc, true);
        }

        private Task SendBatch(BinaryVectorDocument doc, bool isLast)
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
                                headSent = true;
                                read = requestSize.Length;
                                Array.Copy(requestSize, buffer, read);
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

                ClientSocket.Receive(buffer);
            });
        }        
    }
}