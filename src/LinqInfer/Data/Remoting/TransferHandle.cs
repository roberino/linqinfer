using LinqInfer.Maths;
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
            return SendBatch(data, false);
        }

        public Task End()
        {
            return SendBatch(Enumerable.Empty<ColumnVector1D>(), true);
        }

        private Task SendBatch(IEnumerable<ColumnVector1D> data, bool isLast)
        {
            return Task.Factory.StartNew(() =>
            {
                var doc = new DataBatch()
                {
                    Id = Id,
                    ClientId = ClientId,
                    BatchNum = (_batchIndex++),
                    KeepAlive = !isLast,
                    OperationType = OperationType
                };

                foreach (var vec in data)
                {
                    doc.Vectors.Add(vec);
                }

                var buffer = new byte[BufferSize];

                using (var ms = new MemoryStream())
                {
                    doc.Save(ms);

                    ms.Flush();
                    ms.Position = 0;

                    var waitHandle = new ManualResetEvent(false);
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
            });
        }        
    }
}