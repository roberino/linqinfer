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

        public int Write(Stream input, TcpResponseHeader header = null)
        {
            var len = input.Length - input.Position;

            if (header == null) header = new TcpResponseHeader(() => len);

            var head = header.GetBytes();

            Array.Copy(head, _writeBuffer, head.Length);

            var offset = head.Length;

            int read = 0;
            int sent = 0;
            int sending = 0;

            DebugOutput.Log("Sending {0} bytes", len);

            _socket.SendBufferSize = _writeBuffer.Length;

            while (input.Position < len || offset > 0)
            {
                if (len > 0)
                {
                    read = input.Read(_writeBuffer, offset, _writeBuffer.Length - offset);

                    if (read == 0 && offset == 0) break;
                }

                sending = read + offset;

                if (sending < _socket.SendBufferSize)
                {
                    _socket.SendBufferSize = sending;
                }

                _socket.Send(_writeBuffer, sending, SocketFlags.None);

                offset = 0;
                sent += read;
            }

            return sent;
        }

        public async Task<long> WriteAsync(Stream input, TcpResponseHeader header = null)
        {
            var stream = new NetworkStream(_socket);

            var len = input.Length - input.Position;

            if (header == null) header = new TcpResponseHeader(() => len);

            var head = header.GetBytes();

            await stream.WriteAsync(head, 0, head.Length);

            await input.CopyToAsync(stream, _writeBuffer.Length);

            return len + head.Length;
        }

        public async Task<TcpReceiveContext> ReadAsync()
        {
            var socketStream = new NetworkStream(_socket);
            var receivedStream = new MemoryStream();
            var read = 0;

            read = await socketStream.ReadAsync(_readBuffer, 0, _readBuffer.Length);

            var header = new TcpRequestHeader(_readBuffer);

            if ((read - header.HeaderLength) > 0)
                receivedStream.Write(_readBuffer, header.HeaderLength, read - header.HeaderLength);

            while (receivedStream.Position < header.ContentLength)
            {
                read = await socketStream.ReadAsync(_readBuffer, 0, _readBuffer.Length);

                receivedStream.Write(_readBuffer, 0, read);
            }

            DebugOutput.Log("Received {0} bytes", receivedStream.Position);

            receivedStream.Position = 0;

            return new TcpReceiveContext(_socket, _readBuffer.Length)
            {
                Header = header,
                ReceivedData = receivedStream
            };
        }
    }
}