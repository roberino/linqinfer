using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class NetworkLayer : ILayer
    {
        private readonly IList<INeuron> _neurons;
        private readonly Func<int, IList<INeuron>> _neuronsFactory;
        private readonly LayerSpecification _spec;

        public NetworkLayer(
            int inputVectorSize,
            int neuronCount,
            ActivatorFunc activator,
            ILossFunction lossFunction,
            Func<int, INeuron> neuronFactory = null)
            : this(inputVectorSize, new LayerSpecification(neuronCount, activator, lossFunction, new Range(1, -1)))
        {
        }

        public NetworkLayer(int inputVectorSize, LayerSpecification specification)
        {
            _spec = ArgAssert.AssertNonNull(specification, nameof(specification));

            var nf = specification.NeuronFactory;

            _neuronsFactory = new Func<int, IList<INeuron>>((c) =>
            {
                return Enumerable.Range(1, c).Select(n =>
                {
                    var nx = nf(inputVectorSize);
                    nx.Activator = specification.Activator.Activator;
                    return nx;
                }).ToList();
            });

            _neurons = _neuronsFactory(specification.LayerSize);

            InputVectorSize = inputVectorSize;

            LastOutput = Vector.UniformVector(_neurons.Count, 0);
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;

        public INetworkSignalFilter Successor { get; set; }

        public int InputVectorSize { get; }

        public int Size => _neurons.Count;

        public ActivatorFunc Activator => _spec.Activator;

        public ILossFunction LossFunction => _spec.LossFunction;

        public IVector LastOutput { get; internal set; }

        public INeuron this[int index]
        {
            get
            {
                return _neurons[index];
            }
        }

        public Matrix ExportData()
        {
            var vectors = _neurons.Select(n => n.Export());
            return new Matrix(vectors);
        }

        public virtual IVector Process(IVector input)
        {
            IVector outputVector;

            if (_neurons.Any())
            {
                if (_spec.ParallelProcess)
                {
                    var outputItems = _neurons.AsParallel().ForEach(n =>
                    {
                        return n.Evaluate(input);
                    });

                    outputVector = new ColumnVector1D(outputItems.ToArray(outputItems.Count));
                }
                else
                {
                    outputVector = new ColumnVector1D(_neurons.Select(n => n.Evaluate(input)).ToArray(_neurons.Count));
                }
            }
            else
            {
                outputVector = input;
            }

            if (_spec.OutputTransformation != null)
            {
                outputVector = _spec.OutputTransformation.Apply(outputVector);
            }

            LastOutput = outputVector;

            return (Successor == null) ? outputVector : Successor.Process(outputVector);
        }

        public void Grow(int numberOfNewNeurons = 1)
        {
            Contract.Assert(numberOfNewNeurons > 0);

            var newNeurons = _neuronsFactory(numberOfNewNeurons);

            foreach (var n in newNeurons) _neurons.Add(n);
        }

        public void Prune(Func<INeuron, bool> predicate)
        {
            var toBeRemoved = _neurons.Where(n => predicate(n)).ToList();

            foreach (var n in toBeRemoved) _neurons.Remove(n);
        }

        public ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func)
        {
            int i = 0;
            var result = new ColumnVector1D(_neurons.Select(n => func(n, i++)).ToArray(_neurons.Count));
            OnCalculate(result);
            return result;
        }

        public IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func)
        {
            return _neurons.ForEach(func);
        }

        private void OnCalculate(ColumnVector1D vector)
        {
            Calculation?.Invoke(this, new ColumnVector1DEventArgs(vector));
        }

        public ILayer Clone(bool deep)
        {
            var layer = new NetworkLayer(InputVectorSize, _neurons.Count, Activator, LossFunction);

            layer._neurons.Clear();

            foreach(var neuron in _neurons)
            {
                layer._neurons.Add(neuron.Clone(deep));
            }

            if (layer.Successor != null)
            {
                layer.Successor = layer.Successor.Clone(deep);
            }

            return layer;
        }

        public object Clone() => Clone(true);

        INetworkSignalFilter ICloneableObject<INetworkSignalFilter>.Clone(bool deep) => Clone(deep);

        public BinaryVectorDocument Export()
        {
            var layerDoc = new BinaryVectorDocument();

            layerDoc.Properties["Size"] = Size.ToString();

            // layerDoc.Children.Add(_spec.ToVectorDocument());

            foreach (var neuron in _neurons)
            {
                layerDoc.Vectors.Add(neuron.Export());
            }

            return layerDoc;
        }

        public void Import(BinaryVectorDocument data)
        {
            int i = 0;

            foreach (var neuron in _neurons)
            {
                neuron.Import(data.Vectors[i++].ToColumnVector());
            }
        }
    }
}