using LinqInfer.Data;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class MultilayerNetwork :
        ICloneableObject<MultilayerNetwork>, INetworkModel, IHasNetworkTopology
    {
        IVector _lastOutput;
        INetworkSignalFilter _outputModule;

        public MultilayerNetwork(NetworkSpecification specification, IDictionary<string, string> properties = null)
        {
            specification.Validate();

            Specification = specification;

            Properties = properties ?? new Dictionary<string, string>();

            var conf = new NetworkTopologyBuilder(Specification)
                .CreateConfiguration();

            _outputModule = conf.output;

            RootModule = conf.root;
        }

        public IDictionary<string, string> Properties { get; }

        public NetworkSpecification Specification { get; }

        public IEnumerable<Matrix> ExportRawData()
        {
            var data = new List<Matrix>();

            ForwardPropagate(x =>
            {
                if (x is ILayer layer)
                {
                    data.Add(layer.ExportWeights());
                }
            });

            return data;
        }

        public void ForwardPropagate(Action<INetworkSignalFilter> work)
        {
            RootModule.ForwardPropagate(work);
        }

        public double BackwardPropagate(IVector targetOutput)
        {
            var lf = Specification.Output.LossFunction;
            var dr = Activators.None().Derivative;

            var errAndLoss = lf.Calculate(_lastOutput, targetOutput, dr);

            _outputModule.BackwardPropagate(errAndLoss.DerivativeError);

            return errAndLoss.Loss;
        }

        public IVector Apply(IVector vector)
        {
            RootModule.Receive(vector);
            
            if (Specification.Output.OutputTransformation != null)
            {
                _lastOutput = Specification.Output.OutputTransformation.Apply(_outputModule.Output);
            }
            else
            {
                _lastOutput = _outputModule.Output;
            }

            return _lastOutput;
        }

        public int InputSize => Specification.InputVectorSize;

        public int OutputSize => _outputModule.Output.Size;

        /// <summary>
        /// Exports the raw data
        /// </summary>
        public PortableDataDocument ExportData()
        {
            return new MultilayerNetworkExporter().Export(this);
        }

        public static MultilayerNetwork CreateFromData(PortableDataDocument doc)
        {
            return new MultilayerNetworkExporter().Import(doc);
        }

        public override string ToString()
        {
            var s = string.Empty;
            
            ForwardPropagate(x => s += $"/{x.Id}");

            return $"Network({Specification.InputVectorSize}):{s}";
        }

        public MultilayerNetwork Clone(bool deep) => CreateFromData(ExportData());

        public object Clone() => Clone(true);

        public Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            return new NetworkTopologyExporter(this).ExportAsync(visualSettings, store);
        }

        internal INetworkSignalFilter RootModule { get; }
    }
}