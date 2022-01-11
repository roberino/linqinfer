using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    [DebuggerDisplay("in {InputSize} out {Size}")]
    class NeuronCluster
    {
        readonly IWorkOrchestrator _workOrchestrator;
        readonly Func<int, int, IList<INeuron>> _neuronsFactory;

        double[] _buffer;
        double[] _errorBuffer;

        public NeuronCluster(
            Func<int, INeuron> neuronFactory,
            ActivatorExpression activator,
            IWorkOrchestrator workOrchestrator = null)
        {
            _workOrchestrator = workOrchestrator ?? WorkOrchestrator.Default;

            _neuronsFactory = (c, i) =>
            {
                return Enumerable.Range(1, c).Select(n =>
                {
                    var nx = neuronFactory(i);
                    nx.Activator = activator.Activator;
                    return nx;
                }).ToList();
            };

            _buffer = new double[0];
            _errorBuffer = new double[0];

            Neurons = new List<INeuron>();
        }

        public double[] Evaluate(IVector input, bool parallelProcess)
        {
            if (parallelProcess)
            {
                var outputItems = Neurons.AsParallel()
                    .ForEach(n => n.Evaluate(input));

                return outputItems.ToArray(_buffer);
            }

            return Neurons.Select(n => n.Evaluate(input)).ToArray(_buffer);
        }

        public ValueTask<Vector> EvaluateError(Vector error)
        {
            return _workOrchestrator.EnqueueWork(() =>
            {
                var nextError = _errorBuffer;

                for (var i = 0; i < nextError.Length; i++)
                {
                    nextError[i] = ForEachNeuron((nk, k) =>
                        error[k] * nk[i]);
                }

                return new Vector(nextError);
            });
        }

        public double ForEachNeuron(Func<INeuron, int, double> func)
        {
            var result = 0d;

            for (var i = 0; i < Neurons.Count; i++)
            {
                result += func(Neurons[i], i);
            }

            return result;
        }

        public ValueTask<double> ForEachNeuronAsync(Func<INeuron, int, double> func)
        {
            return new ValueTask<double>(ForEachNeuron(func));

            //var i = 0;

            //var tasks = Neurons
            //    .Select(n => (n, i: i++))
            //    .Select(x => _workOrchestrator.EnqueueWork(() => func(x.n, x.i)).AsTask())
            //    .ToList();

            //var results = await Task.WhenAll(tasks);

            //return results.Sum();
        }

        public void Resize(int inputSize, int neuronCount)
        {
            Neurons.Clear();

            foreach (var n in _neuronsFactory(neuronCount, inputSize))
            {
                Neurons.Add(n);
            }

            InputSize = inputSize;

            _buffer = new double[neuronCount];
            _errorBuffer = new double[inputSize];
        }

        public IList<INeuron> Neurons { get; }

        public int InputSize { get; private set; }

        public int Size => Neurons.Count;

        public Matrix ExportData()
        {
            var vectors = Neurons.Select(n => n.Export());
            return new Matrix(vectors);
        }

        public void Grow(int numberOfNewNeurons = 1)
        {
            ArgAssert.Assert(() => numberOfNewNeurons > 0, nameof(numberOfNewNeurons));

            var newNeurons = _neuronsFactory(numberOfNewNeurons, InputSize);

            foreach (var n in newNeurons) Neurons.Add(n);
        }

        public void Prune(Func<INeuron, bool> predicate)
        {
            var toBeRemoved = Neurons.Where(predicate).ToList();

            foreach (var n in toBeRemoved) Neurons.Remove(n);
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