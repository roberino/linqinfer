using LinqInfer.Data;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkLayer : ILayer
    {
        readonly NeuronCluster _neuronCluster;
        readonly NetworkLayerSpecification _spec;
        Vector _output;

        public NetworkLayer(int inputVectorSize, NetworkLayerSpecification specification)
            : this(inputVectorSize, specification, new NeuronCluster(inputVectorSize, specification.LayerSize,
                specification.NeuronFactory,
                specification.Activator))
        {
        }

        protected NetworkLayer(
            int inputVectorSize,
            NetworkLayerSpecification specification,
            NeuronCluster neuronCluster)
        {
            _spec = ArgAssert.AssertNonNull(specification, nameof(specification));

            _neuronCluster = neuronCluster;

            InputVectorSize = inputVectorSize;

            _output = Vector.UniformVector(_neuronCluster.Size, 0);
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;

        public INetworkSignalFilter Successor { get; set; }

        public int InputVectorSize { get; }

        public int Size => _neuronCluster.Size;

        public ActivatorExpression Activator => _spec.Activator;

        public ILossFunction LossFunction => _spec.LossFunction;

        public WeightUpdateRule WeightUpdateRule => _spec.WeightUpdateRule;

        public IVector Output => _output;

        public INeuron this[int index] => _neuronCluster.Neurons[index];

        public Matrix ExportData() => _neuronCluster.ExportData();

        public virtual IVector Process(IVector input)
        {
            if (_neuronCluster.Neurons.Any())
            {
                if (_spec.ParallelProcess)
                {
                    var outputItems = _neuronCluster.Neurons.AsParallel().ForEach(n =>
                    {
                        return n.Evaluate(input);
                    });

                    _output.Overwrite(outputItems);
                }
                else
                {
                    _output.Overwrite(_neuronCluster.Neurons.Select(n => n.Evaluate(input)));
                }
            }
            else
            {
                _output.Overwrite(input.ToColumnVector());
            }

            if (_spec.OutputTransformation != null)
            {
                _output.Overwrite(_spec.OutputTransformation.Apply(_output).ToColumnVector().GetUnderlyingArray());
            }

            return (Successor == null) ? _output : Successor.Process(_output);
        }

        public void Grow(int numberOfNewNeurons = 1) => _neuronCluster.Grow(numberOfNewNeurons);

        public void Prune(Func<INeuron, bool> predicate) => _neuronCluster.Prune(predicate);

        public ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func)
        {
            int i = 0;
            var result = new ColumnVector1D(_neuronCluster.Neurons.Select(n => func(n, i++)).ToArray(_neuronCluster.Size));
            OnCalculate(result);
            return result;
        }

        public IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func) => _neuronCluster.ForEachNeuron(func);

        void OnCalculate(ColumnVector1D vector)
        {
            Calculation?.Invoke(this, new ColumnVector1DEventArgs(vector));
        }

        public ILayer Clone(bool deep)
        {
            var layer = new NetworkLayer(InputVectorSize, _spec);

            layer._neuronCluster.Neurons.Clear();

            foreach(var neuron in _neuronCluster.Neurons)
            {
                layer._neuronCluster.Neurons.Add(neuron.Clone(deep));
            }

            if (layer.Successor != null)
            {
                layer.Successor = layer.Successor.Clone(deep);
            }

            return layer;
        }

        public object Clone() => Clone(true);

        INetworkSignalFilter ICloneableObject<INetworkSignalFilter>.Clone(bool deep) => Clone(deep);

        public PortableDataDocument Export() => _neuronCluster.Export();

        public void Import(PortableDataDocument data) => _neuronCluster.Import(data);
    }
}