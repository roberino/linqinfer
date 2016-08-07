using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    [Serializable]
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

        public MultilayerNetwork(int inputVectorSize, ActivatorFunc activator = null) : this(inputVectorSize, null, activator, null)
        {
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
            var bs = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            bs.Serialize(output, this);

            output.Flush();
        }

        protected virtual void OnLoad()
        {
            var activator = Activators.Create(Parameters.Activator.Name, Parameters.Activator.Parameter);

            Parameters.Activator = activator;

            ForEachLayer(x =>
            {
                if (x is NetworkLayer)
                {
                    ((NetworkLayer)x).ForEachNeuron((n, i) =>
                    {
                        if (n is NeuronBase)
                        {
                            ((NeuronBase)n).Activator = activator.Activator;
                        }
                        return 0;
                    }).ToList();
                }
                return 0;
            }).ToList();
        }

        public static MultilayerNetwork Load(Stream input)
        {
            var bs = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            var network = (MultilayerNetwork)bs.Deserialize(input);

            network.OnLoad();

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