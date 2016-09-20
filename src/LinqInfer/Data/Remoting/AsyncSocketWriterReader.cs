using LinqInfer.Utility;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class AsyncSocketWriterReader
    {
        private readonly Socket _socket;
        private readonly byte[] _readBuffer;
        private readonly byte[] _writeBuffer;

        public AsyncSocketWriterReader(Socket socket, int bufferSize = 1024)
        {
            _socket = socket;
            _readBuffer = new byte[bufferSize];
            _writeBuffer = new byte[bufferSize];

            _socket.SendBufferSize = bufferSize;
            _socket.ReceiveBufferSize = bufferSize;
        }

        public int Write(Stream input)
        {
            var len = input.Length - input.Position;

            var head = BitConverter.GetBytes(len);

            Array.Copy(head, _writeBuffer, head.Length);

            var offset = head.Length;

            int read = 0;
            int sent = 0;

            DebugOutput.Log("Sending {0} bytes", len);

            while (input.Position < len || offset > 0)
            {
                if (len > 0)
                {
                    read = input.Read(_writeBuffer, offset, _writeBuffer.Length - offset);

                    if (read == 0 && offset == 0) break;
                }

                _socket.Send(_writeBuffer, read + offset, SocketFlags.None);

                offset = 0;
                sent += read;
            }

            return sent;
        }

        public Task<int> WriteAsync(Stream input)
        {
            return Task<int>.Factory.StartNew(() =>
            {
                return Write(input);
            });
        }

        public Task<Stream> ReadAsync()
        {
            return Task<Stream>.Factory.StartNew(() =>
            {
                long receiveHeader = -1;
                int read = -1;
                int offset = 0;
                var receivedStream = new MemoryStream();

                while (true)
                {
                    read = _socket.Receive(_readBuffer);

                    if (receiveHeader == -1)
                    {
                        receiveHeader = BitConverter.ToInt64(_readBuffer, 0);
                        offset = 8;
                    }
                    else
                    {
                        offset = 0;
                    }

                    if (receiveHeader == 0) break;
                    
                    receivedStream.Write(_readBuffer, offset, read - offset);

                    if (read == 0 || receivedStream.Length >= receiveHeader) break;
                }

                if (receivedStream.Length > receiveHeader)
                {
                    throw new InvalidOperationException("Invalid data received");
                }

                //DebugOutput.Log("Received {0} bytes", receivedStream.Length);

                receivedStream.Position = 0;

                return receivedStream;
            });
        }
    }
}