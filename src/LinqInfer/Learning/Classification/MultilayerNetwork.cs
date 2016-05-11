using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    [Serializable]
    internal class MultilayerNetwork
    {
        private readonly int _inputVectorSize;
        private readonly Func<int, Range, INeuron> _neuronFactory;
        private INetworkSignalFilter _rootLayer;
        private NetworkParameters _parameters;
        private bool initd;

        public MultilayerNetwork(int inputVectorSize, int[] neuronSizes, ActivatorFunc activator = null, Func<int, Range, INeuron> neuronFactory = null)
        {
            _inputVectorSize = inputVectorSize;
            _neuronFactory = neuronFactory;

            _parameters = new NetworkParameters(inputVectorSize, neuronSizes, activator);

            initd = false;
        }

        public MultilayerNetwork(NetworkParameters parameters)
        {
            _inputVectorSize = parameters.LayerSizes[0];
            _parameters = parameters;
            initd = false;
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

        public void Initialise(params int[] neuronsPerLayer)
        {
            if (neuronsPerLayer == null || neuronsPerLayer.Length == 0) neuronsPerLayer = new[] { _inputVectorSize, _inputVectorSize };

            _parameters.LayerSizes = neuronsPerLayer;

            InitialiseLayers();
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

            foreach (var n in _parameters.LayerSizes.Where(x => x > 0))
            {
                var prev = next;

                if (prev == null)
                {
                    next = new NetworkLayer(_inputVectorSize, _inputVectorSize, _parameters.Activator, neuronFactory);
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
            return (reverse ? Layers.Reverse() : Layers).Select(l => func(l));
        }

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            if (!initd) InitialiseLayers();

            var res = _rootLayer.Process(input);

            // Debugger.Log("{0} => {1}", input.ToCsv(2), res.ToCsv(2));

            return res;
        }

        public IEnumerable<ILayer> Layers
        {
            get
            {
                var next = _rootLayer as ILayer;

                while(next != null)
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
            foreach(var layer in Layers)
            {
                s += "[Layer " + layer.Size + "]";
            }

            return string.Format("Network({0}):{1}", _inputVectorSize, s);
        }
    }
}