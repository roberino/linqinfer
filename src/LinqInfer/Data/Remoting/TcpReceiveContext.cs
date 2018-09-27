using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace LinqInfer.Data.Remoting
{
    class TcpReceiveContext : IDisposable
    {
        public TcpReceiveContext(Socket client)
        {
            ClientSocket = client;
            Cleanup = new List<IDisposable>();
        }

        public TcpRequestHeader Header { get; internal set; }

        public Socket ClientSocket { get; }

        public Stream ReceivedData { get; internal set; }

        public IList<IDisposable> Cleanup { get; }

        public void Dispose()
        {
            foreach (var item in Cleanup)
            {
                try
                {
                    item.Dispose();
                }
                catch
                {

                }
            }
            if (ReceivedData != null) ReceivedData.Dispose();
        }
    }
}