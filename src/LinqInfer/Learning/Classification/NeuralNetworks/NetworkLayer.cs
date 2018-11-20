using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkLayer : NetworkModule, ILayer
    {
        readonly NeuronCluster _neuronCluster;
        readonly NetworkLayerSpecification _spec;

        public NetworkLayer(NetworkLayerSpecification specification)
            : this(specification, new NeuronCluster(specification.NeuronFactory, specification.Activator))
        {
        }

        protected NetworkLayer(
            NetworkLayerSpecification specification,
            NeuronCluster neuronCluster) : base(specification)
        {
            _spec = ArgAssert.AssertNonNull(specification, nameof(specification));

            _neuronCluster = neuronCluster;
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;

        public override string Id => $"layer-{Activator.Name}-{_spec.Id}";

        public int Size => _spec.LayerSize;

        public ActivatorExpression Activator => _spec.Activator;

        public ILossFunction LossFunction => _spec.LossFunction;

        public WeightUpdateRule WeightUpdateRule => _spec.WeightUpdateRule;

        public INeuron this[int index] => _neuronCluster.Neurons[index];

        public Matrix ExportWeights() => _neuronCluster.ExportData();

        public override ErrorAndLossVectors CalculateError(IVector targetOutput)
        {
            return LossFunction.Calculate(Output, targetOutput, Activator.Derivative);
        }

        public override void BackwardPropagate(IVector targetOutput, Vector previousError = null)
        {
            foreach (var predecessor in Predecessors)
            {
                Vector nextError;

                if (previousError != null)
                {
                    if (predecessor is ILayer lastLayer)
                    {
                        nextError = ForEachNeuron((n, i) =>
                        {
                            var err = lastLayer.ForEachNeuron((nk, k) =>
                            {
                                return previousError[k] * nk[i];
                            });

                            return err.Sum * Activator.Derivative(n.Output);
                        });
                    }
                    else
                    {
                        throw new NotSupportedException("TODO");
                    }
                }
                else
                {
                    nextError = CalculateError(targetOutput).DerivativeError;
                }

                predecessor.BackwardPropagate(targetOutput, nextError);
            }
        }

        protected override void Initialise(int inputSize)
        {
            _neuronCluster.Resize(ProcessingVectorSize, _spec.LayerSize);

            _output = Vector.UniformVector(_spec.LayerSize, 0);
        }

        protected override double[] Calculate(IVector input)
        {
            if (_spec.ParallelProcess)
            {
                var outputItems = _neuronCluster.Neurons.AsParallel()
                    .ForEach(n => n.Evaluate(input));

                return outputItems.ToArray();
            }

            return _neuronCluster.Neurons.Select(n => n.Evaluate(input)).ToArray();
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

        public override PortableDataDocument ExportData() => _neuronCluster.Export();

        public void Import(PortableDataDocument data) => _neuronCluster.Import(data);

        public override string ToString() => $"{Id}, {_neuronCluster.Size} neurons)";
    }
}