using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class NetworkLayer : ILayer
    {
        private readonly IList<INeuron> _neurons;
        
        private readonly Func<int, IList<INeuron>> _neuronsFactory;

        public NetworkLayer(int inputVectorSize, int neuronCount, ActivatorFunc activator, Func<int, INeuron> neuronFactory = null)
        {
            var nf = neuronFactory ?? (n => new NeuronBase(n));

            _neuronsFactory = new Func<int, IList<INeuron>>((c) =>
            {
                return Enumerable.Range(1, c).Select(n =>
                {
                    var nx = nf(inputVectorSize);
                    nx.Activator = activator.Activator;
                    return nx;
                }).ToList();
            });

            _neurons = _neuronsFactory(neuronCount);
        }

        private NetworkLayer(IEnumerable<INeuron> neurons)
        {
            _neurons = neurons.ToList();
        }

        public virtual ColumnVector1D Process(ColumnVector1D input)
        {
            var outputVect = _neurons.Any() ? new ColumnVector1D(_neurons.Select(n => n.Evaluate(input)).ToArray()) : input;

            return (Successor == null) ? outputVect : Successor.Process(outputVect);
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
            var result = new ColumnVector1D(_neurons.Select(n => func(n, i++)).ToArray());
            OnCalculate(result);
            return result;
        }

        public IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func)
        {
            return _neurons.Select(n => func(n));
        }

        public INetworkSignalFilter Successor { get; set; }

        public int Size
        {
            get
            {
                return _neurons.Count;
            }
        }

        public INeuron this[int index]
        {
            get
            {
                return _neurons[index];
            }
        }

        private void OnCalculate(ColumnVector1D vector)
        {
            var ev = Calculation;

            if (ev != null) ev.Invoke(this, new ColumnVector1DEventArgs(vector));
        }

        public ILayer Clone(bool deep)
        {
            var layer = new NetworkLayer(_neurons.Select(n => n.Clone(true)));

            if (layer.Successor != null)
            {
                layer.Successor = layer.Successor.Clone(true);
            }

            return layer;
        }

        public object Clone()
        {
            return Clone(true);
        }

        INetworkSignalFilter ICloneableObject<INetworkSignalFilter>.Clone(bool deep)
        {
            return Clone(true);
        }

        public BinaryVectorDocument Export()
        {
            var layerDoc = new BinaryVectorDocument();

            layerDoc.Properties["Size"] = Size.ToString();

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
                neuron.Import(data.Vectors[i++]);
            }
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;
    }
}