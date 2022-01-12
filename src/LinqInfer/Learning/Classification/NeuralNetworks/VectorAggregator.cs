using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class VectorAggregator
    {
        readonly VectorAggregationType _aggregationType;
        readonly Func<IEnumerable<IVector>, IVector> _inputOperator;
        readonly Func<IEnumerable<IVector>> _recurrentInputs;
        readonly IList<IVector> _receivedInputs;

        int _expectedSize;
        bool _flush;

        public VectorAggregator(VectorAggregationType aggregationType,
            Func<IEnumerable<IVector>> recurrentInputs = null)
        {
            _aggregationType = aggregationType;
            _inputOperator = aggregationType.CreateOperation();
            _recurrentInputs = recurrentInputs ?? (() => new IVector[0]);
            _receivedInputs = new List<IVector>();
        }

        public IVector LastInput { get; private set; }

        public void SetExpectedSize(int size)
        {
            _expectedSize = size;
        }

        public int? CalculateVectorSize(params int[] inputSizes)
        {
            if (inputSizes.Length > 1)
            {
                switch (_aggregationType)
                {
                    case VectorAggregationType.Concatinate:
                    case VectorAggregationType.HyperbolicTangent:
                        if (inputSizes.Any(x => x == 0))
                        {
                            return null;
                        }
                        return inputSizes.Sum();
                    default:
                        return inputSizes.Max();
                }
            }

            return inputSizes[0];
        }

        public (bool received, IVector data) Receive(IVector input)
        {
            if (_flush)
            {
                _receivedInputs.Clear();
                LastInput = null;
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
                LastInput = aggregateInput[0];

                return LastInput;
            }

            LastInput = _inputOperator(aggregateInput);

            return LastInput;
        }
    }
}