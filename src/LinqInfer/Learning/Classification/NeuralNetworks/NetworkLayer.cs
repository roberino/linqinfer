using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
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

            OutputVector = Vector.UniformVector(_spec.LayerSize, 0);
        }

        public override string Id => $"Layer-{Activator.Name}-{_spec.Id}";

        public int Size => _spec.LayerSize;

        public ActivatorExpression Activator => _spec.Activator;

        public WeightUpdateRule WeightUpdateRule => _spec.WeightUpdateRule;

        public INeuron this[int index] => _neuronCluster.Neurons[index];

        public Matrix ExportWeights() => _neuronCluster.ExportData();

        public override PortableDataDocument ExportData()
        {
            var baseData = base.ExportData();
            var data = _neuronCluster.Export();

            foreach (var prop in data.Properties)
            {
                baseData.Properties[prop.Key] = prop.Value;
            }

            foreach (var prop in data.Vectors)
            {
                baseData.Vectors.Add(prop);
            }

            return baseData;
        }

        public override void ImportData(PortableDataDocument data)
        {
            base.ImportData(data);

            _neuronCluster.Import(data);
        }

        public override string ToString() => $"{Id}, in {_neuronCluster.InputSize} out {_neuronCluster.Size} neurons";

        public void Grow(int numberOfNewNeurons = 1) => _neuronCluster.Grow(numberOfNewNeurons);

        public void Prune(Func<INeuron, bool> predicate) => _neuronCluster.Prune(predicate);

        public override bool IsInitialised => base.IsInitialised && _neuronCluster.Neurons.Any();

        protected override void Initialise(int inputSize)
        {
            _neuronCluster.Resize(ProcessingVectorSize, _spec.LayerSize);

            OutputVector = Vector.UniformVector(_spec.LayerSize, 0);
        }

        protected override Vector ProcessError(Vector error, IVector predecessorOutput)
        {
            var nextError = new double[_neuronCluster.InputSize];

            for (var i = 0; i < nextError.Length; i++)
            {
                nextError[i] = _neuronCluster.ForEachNeuron((nk, k) =>
                    error[k] * nk[i]);
            }

            Adjust(predecessorOutput, error);

            return new Vector(nextError);
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

        void Adjust(IVector previousOutput, Vector layerErrors)
        {
            DebugOutput.LogVerbose($"Adjust from previous {previousOutput.Size} to {this} using errors {layerErrors.Size}");

            _neuronCluster.ForEachNeuron((n, j) =>
            {
                var error = layerErrors[j] * Activator.Derivative(n.Output);

                n.Adjust((w, k) =>
                {
                    var prevOutput = k < 0 ? 1 : previousOutput[k];

                    var wp = new WeightUpdateParameters()
                    {
                        CurrentWeightValue = w,
                        Error = error,
                        PreviousLayerOutput = prevOutput
                    };

                    var wu = WeightUpdateRule.Execute(wp);

                    // DebugOutput.Log($"w = {wu} => error = {wp.Error} previous output = {wp.PreviousLayerOutput}, w = {wp.CurrentWeightValue}");

                    return wu;
                });

                return 0;
            });
        }
    }
}