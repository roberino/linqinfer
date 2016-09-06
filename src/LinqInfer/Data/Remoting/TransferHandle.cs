using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class TransferHandle
    {
        private int _batchIndex;

        public TransferHandle(string operationType, SocketState state, Action<TransferHandle> onConnect)
        {
            State = state;
            OperationType = operationType;
            OnConnect = onConnect ?? (h => { });
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; private set; }

        public string OperationType { get; private set; }

        public SocketState State { get; private set; }

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
                var doc = new BinaryVectorDocument();

                foreach (var vec in data)
                {
                    doc.Vectors.Add(vec);
                }

                doc.Properties["OpType"] = OperationType;
                doc.Properties["Id"] = Id;
                doc.Properties["Batch"] = (_batchIndex++).ToString();
                doc.Properties["KeepAlive"] = (!isLast).ToString();

                var buffer = new byte[State.Buffer.Length];

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

                        State.ClientSocket.BeginSend(buffer, 0, read, 0,
                        a =>
                        {
                            var handle = (TransferHandle)a.AsyncState;
                            handle.State.ClientSocket.EndSend(a);
                            waitHandle.Set();

                        }, this);

                        waitHandle.WaitOne();
                    }
                }
            });
        }        
    }
}