using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkModule : INetworkSignalFilter
    {
        readonly NetworkModuleSpecification _spec;
        readonly VectorAggregator _inputAggregator;
        readonly VectorAggregator _errorAggregator;

        protected Vector OutputVector;

        int? _inputVectorSize;

        public NetworkModule(NetworkModuleSpecification spec)
        {
            _spec = spec;
            _inputAggregator = new VectorAggregator(spec.InputOperator, () => RecurrentInputs.Select(i => i.Output));
            _errorAggregator = new VectorAggregator(VectorAggregationType.Add);

            OutputVector = Vector.UniformVector(0, 0);
            Successors = new List<NetworkModule>();
            Predecessors = new List<NetworkModule>();
            RecurrentInputs = new List<NetworkModule>();
        }

        public NetworkModule(int inputVectorSize) : this(new NetworkModuleSpecification(-1))
        {
            Initialise(inputVectorSize);
        }

        public virtual string Id => $"Module-{_spec.InputOperator}-{(_spec.Id > -1 ? _spec.Id.ToString() : "Root")}";

        public IEnumerable<INetworkSignalFilter> Inputs => RecurrentInputs.Concat(Predecessors);

        public IList<NetworkModule> Successors { get; }

        public IList<NetworkModule> Predecessors { get; }

        public IList<NetworkModule> RecurrentInputs { get; }

        public int ProcessingVectorSize => _inputVectorSize.GetValueOrDefault();

        public IVector Output => OutputVector;

        public virtual void ImportData(PortableDataDocument doc)
        {
        }

        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();
            doc.Properties[nameof(Id)] = Id;
            doc.SetType(this);
            doc.SetName(GetType().Name);
            return doc;
        }

        public virtual bool IsInitialised => OutputVector.Size > 0; 

        public virtual bool Initialise(params int[] inputSizes)
        {
            var outputSize = _inputAggregator.CalculateVectorSize(inputSizes);

            if (!outputSize.HasValue)
            {
                return false;
            }

            _inputVectorSize = outputSize.Value;
            
            Initialise(_inputVectorSize.Value);

            return true;
        }

        public void Receive(IVector input)
        {
            var (received, data) = _inputAggregator.Receive(input);

            if (received)
            {
                Process(data);
            }
        }

        public void ForwardPropagate(Action<INetworkSignalFilter> work)
        {
            work(this);

            foreach (var successor in Successors)
            {
                successor.ForwardPropagate(work);
            }
        }

        public void BackwardPropagate(Action<INetworkSignalFilter> work)
        {
            work(this);

            foreach (var successor in Predecessors)
            {
                successor.BackwardPropagate(work);
            }
        }

        public void BackwardPropagate(Vector error)
        {
            var aggregateError = _errorAggregator.Receive(error);
            var lastInput = _inputAggregator.LastInput;

            if (aggregateError.received && lastInput != null)
            {
                var nextError = ProcessError(error, lastInput);

                foreach (var predecessor in Predecessors)
                {
                    predecessor.BackwardPropagate(nextError);
                }
            }
        }

        public override string ToString() => $"{Id} in {_inputVectorSize.GetValueOrDefault(Output.Size)} out {Output.Size}";

        protected virtual Vector ProcessError(Vector error, IVector predecessorOutput)
        {
            return error;
        }

        protected virtual double[] Calculate(IVector input)
        {
            return input.ToColumnVector().GetUnderlyingArray();
        }

        protected virtual void Initialise(int inputSize)
        {
            OutputVector = Vector.UniformVector(inputSize, 0);

            _inputAggregator.SetExpectedSize(Predecessors.Count);
            _errorAggregator.SetExpectedSize(Successors.Count);
        }

        void Process(IVector aggregatedInput)
        {
            DebugOutput.LogVerbose($"Process {_spec.Id} {Output.Size} {_spec.InputOperator}");

            var previousOutput = (Vector)Output;

            previousOutput.Overwrite(Calculate(aggregatedInput));
            
            foreach (var successor in Successors)
            {
                successor.Receive(previousOutput);
            }
        }
    }
}