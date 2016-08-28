﻿using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetwork : ICloneableObject<MultilayerNetwork>
    {
        private readonly Func<int, Range, INeuron> _neuronFactory;

        private INetworkSignalFilter _rootLayer;
        private NetworkParameters _parameters;
        private bool initd;

        public MultilayerNetwork(Stream input)
        {
            var n = Load(input);
            _neuronFactory = n._neuronFactory;
            _rootLayer = n._rootLayer;
            _parameters = n._parameters;
            initd = true;
        }

        public MultilayerNetwork(NetworkParameters parameters)
        {
            parameters.Validate();

            _parameters = parameters;
            initd = false;
        }

        internal MultilayerNetwork(int inputVectorSize, int[] neuronSizes, ActivatorFunc activator = null, Func<int, Range, INeuron> neuronFactory = null)
        {
            _neuronFactory = neuronFactory;

            _parameters = new NetworkParameters(new int[] { inputVectorSize }.Concat(neuronSizes).ToArray(), activator);

            initd = false;
        }

        private MultilayerNetwork(NetworkParameters parameters, Func<int, Range, INeuron> neuronFactory, INetworkSignalFilter rootLayer, int inputVectorSize)
        {
            _parameters = parameters;
            _neuronFactory = neuronFactory;
            _rootLayer = rootLayer;
            initd = true;
        }

        public NetworkParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }

        private void InitialiseLayers()
        {
            INetworkSignalFilter next = null;

            int lastN = 0;

            Func<int, INeuron> neuronFactory;

            if (_neuronFactory == null)
                neuronFactory = (x => new NeuronBase(x, _parameters.InitialWeightRange));
            else
                neuronFactory = (x => _neuronFactory(x, _parameters.InitialWeightRange));

            foreach (var n in _parameters.LayerSizes.Where(x => x > 0)) // Don't create empty layers
            {
                var prev = next;

                if (prev == null)
                {
                    next = new NetworkLayer(Parameters.InputVectorSize, Parameters.InputVectorSize, _parameters.Activator, neuronFactory);
                    _rootLayer = next;
                }
                else
                {
                    next = new NetworkLayer(lastN, n, _parameters.Activator, neuronFactory);
                    prev.Successor = next;
                }

                lastN = n;
            }

            initd = true;
        }

        public ILayer LastLayer { get { return Layers.Reverse().First(); } }

        public IEnumerable<T> ForEachLayer<T>(Func<ILayer, T> func, bool reverse = true)
        {
            if (!initd) InitialiseLayers();

            return (reverse ? Layers.Reverse() : Layers).Select(l => func(l));
        }

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            if (!initd) InitialiseLayers();

            var res = _rootLayer.Process(input);

            // Debugger.Log("{0} => {1}", input.ToCsv(2), res.ToCsv(2));

            return res;
        }

        /// <summary>
        /// Reduces the networks input parameters and associated weights to improve it's efficiency.
        /// </summary>
        /// <param name="inputIndexes">One or more input indexes (base zero)</param>
        public void PruneInputs(params int[] inputIndexes)
        {
            if (inputIndexes == null || inputIndexes.Length == 0) throw new ArgumentException("No inputs recieved");

            var newSize = Enumerable.Range(0, Parameters.InputVectorSize).Except(inputIndexes).Count();

            ForEachLayer(l =>
            {
                l.ForEachNeuron(n =>
                {
                    n.PruneWeights(inputIndexes);
                    return 1;
                }).ToList();

                return 1;
            }, false).ToList();

            Parameters.InputVectorSize = newSize;
        }

        public IEnumerable<ILayer> Layers
        {
            get
            {
                if (!initd) InitialiseLayers();

                var next = _rootLayer as ILayer;

                while (next != null)
                {
                    yield return next;

                    next = next.Successor as ILayer;
                }
            }
        }

        public void Save(Stream output)
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["Activator"] = _parameters.Activator.Name;
            doc.Properties["ActivatorParameter"] = _parameters.Activator.Parameter.ToString();
            doc.Properties["InitialWeightRangeMin"] = _parameters.InitialWeightRange.Min.ToString();
            doc.Properties["InitialWeightRangeMax"] = _parameters.InitialWeightRange.Max.ToString();
            doc.Properties["LearningRate"] = _parameters.LearningRate.ToString();
            
            doc.Properties["Label"] = "Network";

            int i = 0;

            foreach (var layer in Layers)
            {
                i++;

                var layerDoc = layer.Export();

                layerDoc.Properties["Label"] = "Layer " + i;

                doc.Children.Add(layerDoc);
            }

            doc.Save(output);

            output.Flush();
        }

        public static MultilayerNetwork Load(Stream input)
        {
            var doc = new BinaryVectorDocument();

            doc.Load(input);

            var activator = Activators.Create(doc.Properties["Activator"], double.Parse(doc.Properties["ActivatorParameter"]));
            var layerSizes = doc.Children.Select(c => int.Parse(c.Properties["Size"])).ToArray();

            var network = new MultilayerNetwork(new NetworkParameters(layerSizes, activator)
            {
                LearningRate = double.Parse(doc.Properties["LearningRate"]),
                InitialWeightRange = new Range(double.Parse(doc.Properties["InitialWeightRangeMax"]), double.Parse(doc.Properties["InitialWeightRangeMin"]))
            });
            
            int i = 0;

            foreach (var layer in network.Layers)
            {
                layer.Import(doc.Children[i++]);
            }

            return network;
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var layer in Layers)
            {
                s += "[Layer " + layer.Size + "]";
            }

            return string.Format("Network({0}):{1}", Parameters.InputVectorSize, s);
        }

        public MultilayerNetwork Clone(bool deep)
        {
            return new MultilayerNetwork(_parameters.Clone(deep), _neuronFactory, _rootLayer.Clone(deep), Parameters.InputVectorSize);
        }

        public object Clone()
        {
            return Clone(true);
        }
    }
}