using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class VectorAggregator
    {
        readonly Func<IEnumerable<IVector>, IVector> _inputOperator;
        readonly Func<IEnumerable<IVector>> _recurrentInputs;
        readonly IList<IVector> _receivedInputs;

        int _expectedSize;
        bool _flush;

        public VectorAggregator(Func<IEnumerable<IVector>, IVector> inputOperator,
            Func<IEnumerable<IVector>> recurrentInputs)
        {
            _inputOperator = inputOperator;
            _recurrentInputs = recurrentInputs;
            _receivedInputs = new List<IVector>();
        }

        public void SetExpectedSize(int size)
        {
            _expectedSize = size;
        }

        public (bool received, IVector data) Receive(IVector input)
        {
            if (_flush)
            {
                _receivedInputs.Clear();
                _flush = false;
            }

            _receivedInputs.Add(input);

            if (_receivedInputs.Count >= _expectedSize)
            {
                return (true, RetrieveInput());
            }

            return (false, null);
        }

        IVector RetrieveInput()
        {
            var aggregateInput = new List<IVector>();

            foreach (var item in _recurrentInputs())
            {
                aggregateInput.Add(item);
            }

            foreach (var item in _receivedInputs)
            {
                aggregateInput.Add(item);
            }

            _flush = true;

            if (aggregateInput.Count == 1)
            {
                return aggregateInput[0];
            }

            return _inputOperator(aggregateInput);
        }
    }
}