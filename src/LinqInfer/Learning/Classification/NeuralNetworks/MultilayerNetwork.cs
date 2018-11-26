﻿using LinqInfer.Data;
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
        readonly object _lockObj = new object();
        IVector _lastOutput;
        INetworkSignalFilter _rootLayer;
        INetworkSignalFilter _outputModule;
        bool _initd;

        public MultilayerNetwork(NetworkSpecification specification, IDictionary<string, string> properties = null)
        {
            specification.Validate();

            Specification = specification;

            Properties = properties ?? new Dictionary<string, string>();

            _initd = false;
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

            OutputModule.BackwardPropagate(errAndLoss.DerivativeError);

            return errAndLoss.Loss;
        }

        /// <summary>
        /// Transforms the vector by evaluating the data through the network
        /// </summary>
        public IVector Apply(IVector vector)
        {
            RootModule.Receive(vector);
            
            if (Specification.Output.OutputTransformation != null)
            {
                _lastOutput = Specification.Output.OutputTransformation.Apply(OutputModule.Output);
            }
            else
            {
                _lastOutput = OutputModule.Output;
            }

            return _lastOutput;
        }

        public int InputSize => Specification.InputVectorSize;

        public int OutputSize => OutputModule.Output.Size;

        /// <summary>
        /// Exports the raw data
        /// </summary>
        public PortableDataDocument ExportData()
        {
            return new MultilayerNetworkExporter().Export(this);
        }

        public void ImportData(PortableDataDocument doc)
        {
            var nn = CreateFromData(doc);

            _rootLayer = nn._rootLayer;
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

        public MultilayerNetwork Clone(bool deep)
        {
            var data = ExportData();
            var newNet = new MultilayerNetwork(Specification);

            newNet.ImportData(data);

            return newNet;
        }

        public object Clone()
        {
            return Clone(true);
        }

        public Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            return new NetworkTopologyExporter(this).ExportAsync(visualSettings, store);
        }

        internal INetworkSignalFilter RootModule
        {
            get
            {
                if (!_initd) InitialiseLayers();

                return _rootLayer;
            }
        }

        INetworkSignalFilter OutputModule
        {
            get
            {
                if (!_initd) InitialiseLayers();

                return _outputModule;
            }
        }

        void InitialiseLayers()
        {
            lock (_lockObj)
            {
                if (_initd)
                {
                    return;
                }

                _initd = true;

                var conf = new NetworkTopologyBuilder(Specification)
                    .CreateConfiguration();

                _rootLayer = conf.root;
                _outputModule = conf.output;
                _lastOutput = _outputModule.Output;
            }
        }
    }
}