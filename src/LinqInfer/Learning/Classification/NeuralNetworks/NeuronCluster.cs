using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NeuronCluster
    {
        readonly Func<int, IList<INeuron>> _neuronsFactory;

        public NeuronCluster(int inputVectorSize,
            int neuronCount,
            Func<int, INeuron> neuronFactory,
            ActivatorExpression activator)
        {
            _neuronsFactory = c =>
            {
                return Enumerable.Range(1, c).Select(n =>
                {
                    var nx = neuronFactory(inputVectorSize);
                    nx.Activator = activator.Activator;
                    return nx;
                }).ToList();
            };

            Neurons = _neuronsFactory(neuronCount);
        }

        public IList<INeuron> Neurons { get; }

        public int Size => Neurons.Count;

        public Matrix ExportData()
        {
            var vectors = Neurons.Select(n => n.Export());
            return new Matrix(vectors);
        }

        public void Grow(int numberOfNewNeurons = 1)
        {
            Contract.Assert(numberOfNewNeurons > 0);

            var newNeurons = _neuronsFactory(numberOfNewNeurons);

            foreach (var n in newNeurons) Neurons.Add(n);
        }

        public void Prune(Func<INeuron, bool> predicate)
        {
            var toBeRemoved = Neurons.Where(predicate).ToList();

            foreach (var n in toBeRemoved) Neurons.Remove(n);
        }

        public IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func)
        {
            return Neurons.ForEach(func);
        }

        public PortableDataDocument Export()
        {
            var layerDoc = new PortableDataDocument();

            layerDoc.Properties[nameof(Size)] = Size.ToString();

            foreach (var neuron in Neurons)
            {
                layerDoc.Vectors.Add(neuron.Export());
            }

            return layerDoc;
        }

        public void Import(PortableDataDocument data)
        {
            int i = 0;

            foreach (var neuron in Neurons)
            {
                neuron.Import(data.Vectors[i++].ToColumnVector());
            }
        }
    }
}