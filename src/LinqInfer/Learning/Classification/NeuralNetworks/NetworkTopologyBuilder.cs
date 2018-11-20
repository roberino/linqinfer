using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkTopologyBuilder
    {
        readonly NetworkSpecification _specification;
        readonly Dictionary<int, (NetworkModuleSpecification spec, NetworkModule module)> _moduleLookup;

        public NetworkTopologyBuilder(NetworkSpecification specification)
        {
            _specification = specification;
            _moduleLookup = new Dictionary<int, (NetworkModuleSpecification spec, NetworkModule module)>();
        }

        public (INetworkSignalFilter root, INetworkSignalFilter output) CreateConfiguration()
        {
            _specification.Initialise();

            var root = new NetworkModule(_specification.InputVectorSize);

            var main = Lookup(_specification.Root.Id);

            main.mod.Predecessors.Add(root);

            foreach (var item in _moduleLookup)
            {
                foreach (var inp in item.Value.spec.Connections.Inputs)
                {
                    item.Value.module.RecurrentInputs.Add(_moduleLookup[inp].module);
                }
                foreach (var outp in item.Value.spec.Connections.Outputs)
                {
                    var successor = _moduleLookup[outp].module;

                    item.Value.module.Successors.Add(successor);

                    successor.Predecessors.Add(item.Value.module);
                }
            }

            var output = _moduleLookup[_specification.Output.Id];

            output.module.Initialise(_specification.OutputVectorSize);

            foreach (var _ in _moduleLookup)
            {
                bool missing = false;
                var visited = new ConcurrentDictionary<int, int>();

                main.mod.ForwardPropagate(m =>
                {
                    var nextOutput = m.Output.Size;

                    visited.AddOrUpdate(m.Id, 1, (id, x) =>
                    {
                        DebugOutput.Log($"Visited {m} ({x})");

                        if (x > 3)
                        {
                            DebugOutput.Log("R");
                        }

                        if (x > 10)
                        {
                            throw new System.Exception("XXX");
                        }

                        return x + 1;
                    });

                    if (nextOutput == 0)
                    {
                        var inputs = m
                            .RecurrentInputs.Concat(m.Predecessors);

                        var inputSizes = inputs.Select(x => x.Output.Size).ToArray();

                        missing = !m.Initialise(inputSizes);

                        if(missing) {
                        DebugOutput.Log($"{m}"); 
                            }
                    }
                });

                if (!missing)
                {
                    break;
                }
            }

            return (main.mod, output.module);
        }

        (NetworkModuleSpecification spec, NetworkModule mod) Lookup(int id)
        {
            if (!_moduleLookup.TryGetValue(id, out var items))
            {
                var spec = _specification.Modules.Single(s => s.Id == id);

                var module = CreateModuleOrLayer(spec);

                items = (spec, module);

                _moduleLookup[id] = items;

                foreach (var successor in items.spec.Connections.Outputs)
                {
                    Lookup(successor);
                }
            }

            return items;
        }

        static NetworkModule CreateModuleOrLayer(NetworkModuleSpecification spec)
        {
            if (spec is NetworkLayerSpecification layerSpecification)
            {
                return new NetworkLayer(layerSpecification);
            }

            return new NetworkModule(spec);
        }
    }
}