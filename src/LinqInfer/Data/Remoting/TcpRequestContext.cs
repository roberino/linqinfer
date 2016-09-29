<<<<<<< HEAD
﻿using System.IO;
=======
﻿using System.Collections.Generic;
using System.IO;
>>>>>>> 6074a23c24e2de99dced68dc09ca93eaef1fe5e2
using System.Net.Sockets;

namespace LinqInfer.Data.Remoting
{
    internal class TcpRequestContext
    {
        public TcpRequestContext(Socket client, int bufferSize = 1024)
        {
            ClientSocket = client;
            Buffer = new byte[bufferSize];
            ReceivedData = new MemoryStream();
        }

        public void Reset()
        {
            ContentLength = null;
            ReceivedData.Position = 0;
        }

        public long? ContentLength { get; set; }

        public TcpRequestHeader Header { get; internal set; }

        public Socket ClientSocket { get; private set; }

        public byte[] Buffer { get; private set; }

        public Stream ReceivedData { get; private set; }
    }
}