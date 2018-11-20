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
        readonly Func<IEnumerable<IVector>, IVector> _inputOperator;
        readonly Queue<IVector> _receivedInputs;
        
        protected Vector _output;

        int? _inputVectorSize;

        public NetworkModule(NetworkModuleSpecification spec)
        {
            _spec = spec;
            _output = Vector.UniformVector(0, 0);
            _inputOperator = spec.InputOperator.CreateOperation();
            _receivedInputs = new Queue<IVector>();

            Successors = new List<NetworkModule>();
            Predecessors = new List<NetworkModule>();
            RecurrentInputs = new List<NetworkModule>();
        }

        public NetworkModule(int inputVectorSize) : this(new NetworkModuleSpecification(-1))
        {
            Initialise(inputVectorSize);
        }

        public int Id => _spec.Id;

        public IList<NetworkModule> Successors { get; }

        public IList<NetworkModule> Predecessors { get; }

        public IList<NetworkModule> RecurrentInputs { get; }

        public int ProcessingVectorSize => _inputVectorSize.GetValueOrDefault();

        public IVector Output => _output;
        
        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();
            doc.SetName(nameof(NetworkModule));
            return doc;
        }

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

        protected virtual void Initialise(int inputSize)
        {
            _output = Vector.UniformVector(inputSize, 0);
        }

        public void Receive(IVector input)
        {
            _receivedInputs.Enqueue(input);

            if (_receivedInputs.Count == Predecessors.Count)
            {
                Process();
            }
        }

        public void ForwardPropagate(Action<NetworkModule> work)
        {
            work(this);

            foreach (var successor in Successors)
            {
                successor.ForwardPropagate(work);
            }
        }

        void Process()
        {
            DebugOutput.Log($"Process {_spec.Id} {Output.Size} {_spec.InputOperator}");

            if (_receivedInputs.Count == 0)
            {
                return;
            }

            var previousOutput = (Vector)Output;

            var aggrInput = RetrieveInput();

            previousOutput.Overwrite(Calculate(aggrInput));

            if (_spec.OutputTransformation != null)
            {
                previousOutput.Overwrite(_spec.OutputTransformation.Apply(previousOutput).ToColumnVector().GetUnderlyingArray());
            }
            
            foreach (var successor in Successors)
            {
                successor.Receive(previousOutput);
            }
        }

        protected virtual double[] Calculate(IVector input)
        {
            return input.ToColumnVector().GetUnderlyingArray();
        }

        public override string ToString() => $"Module {_spec.Id} ({_spec.InputOperator})";

        //int CalculateVectorSize()
        //{
        //    if (_inputVectorSize.HasValue)
        //    {
        //        return _inputVectorSize.Value;
        //    }

        //    int size;

        //    var allInputs = RecurrentInputs.Concat(Predecessors);

        //    switch (_spec.InputOperator)
        //    {
        //        case VectorAggregationType.Concatinate:
        //        case VectorAggregationType.HyperbolicTangent:
        //            size = allInputs.Sum(p => p.OutputVectorSize);
        //            break;
        //        default:
        //            size = allInputs.Min(p => p.OutputVectorSize);
        //            break;
        //    }

        //    return size;
        //}

        IVector RetrieveInput()
        {
            var aggrInputs = new List<IVector>();

            foreach (var item in RecurrentInputs)
            {
                aggrInputs.Add(item.Output);
            }

            while (_receivedInputs.Count > 0)
            {
                aggrInputs.Add(_receivedInputs.Dequeue());
            }

            if (aggrInputs.Count == 1)
            {
                return aggrInputs[0];
            }

            return _inputOperator(aggrInputs); 
        }
    }
}