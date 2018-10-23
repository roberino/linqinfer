using LinqInfer.Maths;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class VectorBuffer
    {
        readonly int _bufferSize;
        readonly int _vectorSize;
        readonly Stack<IVector> _buffer;

        public VectorBuffer(int bufferSize, int vectorSize)
        {
            _bufferSize = bufferSize;
            _vectorSize = vectorSize;
        }

        public IList<IVector> PopAndRead()
        {
            var combinedInputItems = new List<IVector>();

            for (var i = 0; i < _bufferSize - _buffer.Count; i++)
            {
                combinedInputItems.Add(new ZeroVector(_vectorSize));
            }

            if (_buffer.Count > _bufferSize)
            {
                combinedInputItems.Add(_buffer.Pop());
            }

            foreach (var item in _buffer)
            {
                combinedInputItems.Add(item);
            }

            return combinedInputItems;
        }

        public void Push(IVector vector)
        {
            _buffer.Push(vector);
        }
    }
}
