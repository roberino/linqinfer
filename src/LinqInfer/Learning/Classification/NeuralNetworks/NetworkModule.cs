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
        readonly VectorAggregator _vectorAggregator;

        protected Vector OutputVector;

        int? _inputVectorSize;

        public NetworkModule(NetworkModuleSpecification spec)
        {
            _spec = spec;
            _vectorAggregator = new VectorAggregator(spec.InputOperator.CreateOperation(), () => RecurrentInputs.Select(i => i.Output));

            OutputVector = Vector.UniformVector(0, 0);
            Successors = new List<NetworkModule>();
            Predecessors = new List<NetworkModule>();
            RecurrentInputs = new List<NetworkModule>();
        }

        public NetworkModule(int inputVectorSize) : this(new NetworkModuleSpecification(-1))
        {
            Initialise(inputVectorSize);
        }

        public virtual string Id => $"Module-{_spec.InputOperator}-{_spec.Id}";

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
            int outputSize;

            if (inputSizes.Length > 1)
            {
                switch (_spec.InputOperator)
                {
                    case VectorAggregationType.Concatinate:
                    case VectorAggregationType.HyperbolicTangent:
                        if(inputSizes.Any(x => x == 0))
                        {
                            return false;
                        }
                        outputSize = inputSizes.Sum();
                        break;
                    default:
                        outputSize = inputSizes.Max();
                        break;
                }
            }
            else
            {
                outputSize = inputSizes[0];
            }

            _inputVectorSize = outputSize;
            
            Initialise(_inputVectorSize.Value);

            return true;
        }

        public void Receive(IVector input)
        {
            var (received, data) = _vectorAggregator.Receive(input);

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

        public virtual void BackwardPropagate(Vector error)
        {
            foreach (var predecessor in Predecessors)
            {
                predecessor.BackwardPropagate(error);
            }
        }

        public override string ToString() => $"{Id} ({Output.Size})";

        protected virtual double[] Calculate(IVector input)
        {
            return input.ToColumnVector().GetUnderlyingArray();
        }

        protected virtual void Initialise(int inputSize)
        {
            OutputVector = Vector.UniformVector(inputSize, 0);

            _vectorAggregator.SetExpectedSize(Predecessors.Count);
        }

        void Process(IVector aggregatedInput)
        {
            DebugOutput.Log($"Process {_spec.Id} {Output.Size} {_spec.InputOperator}");

            var previousOutput = (Vector)Output;

            previousOutput.Overwrite(Calculate(aggregatedInput));
            
            foreach (var successor in Successors)
            {
                successor.Receive(previousOutput);
            }
        }
    }
}