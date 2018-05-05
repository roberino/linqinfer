using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class NetworkLayer : ILayer
    {
        private readonly IList<INeuron> _neurons;
        private readonly Func<int, IList<INeuron>> _neuronsFactory;
        private readonly LayerSpecification _spec;
        private Vector _output;

        public NetworkLayer(
            int inputVectorSize,
            int neuronCount,
            IActivatorFunction activator,
            ILossFunction lossFunction,
            Func<int, INeuron> neuronFactory = null)
            : this(inputVectorSize, new LayerSpecification(neuronCount, activator, lossFunction, DefaultWeightUpdateRule.Create(), Range.MinusOneToOne))
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

            _output = Vector.UniformVector(_neurons.Count, 0);
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;

        public INetworkSignalFilter Successor { get; set; }

        public int InputVectorSize { get; }

        public int Size => _neurons.Count;

        public IActivatorFunction Activator => _spec.Activator;

        public ILossFunction LossFunction => _spec.LossFunction;

        public IWeightUpdateRule WeightUpdateRule => _spec.WeightUpdateRule;

        public IVector LastOutput => _output;

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
            if (_neurons.Any())
            {
                if (_spec.ParallelProcess)
                {
                    var outputItems = _neurons.AsParallel().ForEach(n =>
                    {
                        return n.Evaluate(input);
                    });

                    _output.Overwrite(outputItems);
                }
                else
                {
                    _output.Overwrite(_neurons.Select(n => n.Evaluate(input)));
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

        public PortableDataDocument Export()
        {
            var layerDoc = new PortableDataDocument();

            layerDoc.Properties["Size"] = Size.ToString();

            foreach (var neuron in _neurons)
            {
                layerDoc.Vectors.Add(neuron.Export());
            }

            return layerDoc;
        }

        public void Import(PortableDataDocument data)
        {
            int i = 0;

            foreach (var neuron in _neurons)
            {
                neuron.Import(data.Vectors[i++].ToColumnVector());
            }
        }
    }
}