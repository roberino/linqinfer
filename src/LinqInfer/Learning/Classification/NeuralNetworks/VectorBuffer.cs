using LinqInfer.Maths;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class VectorBuffer
    {
        readonly int _vectorSize;
        readonly Queue<IVector> _buffer;

        public VectorBuffer(int bufferSize, int vectorSize)
        {
            BufferSize = bufferSize;
            _vectorSize = vectorSize;
            _buffer = new Queue<IVector>();
        }
        
        public int BufferSize { get; }

        public IList<IVector> PopAndRead()
        {
            var combinedInputItems = new List<IVector>();

            for (var i = 0; i < BufferSize - _buffer.Count; i++)
            {
                combinedInputItems.Add(new ZeroVector(_vectorSize));
            }

            if (_buffer.Count > BufferSize)
            {
                combinedInputItems.Add(_buffer.Dequeue());
            }

            foreach (var item in _buffer)
            {
                combinedInputItems.Add(item);
            }

            return combinedInputItems;
        }

        public void Push(IVector vector)
        {
            if (BufferSize <= 0)
            {
                return;
            }

            _buffer.Enqueue(vector);
        }
    }
}
