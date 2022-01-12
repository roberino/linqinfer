using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkTopologyBuilder
    {
        const string CircularReferenceMessage = "Circular recurrence detected";
        const string ConfigInvalidMessage = "Cannot create workable network graph";

        readonly NetworkSpecification _specification;
        readonly IWorkOrchestrator _workOrchestrator;
        readonly Dictionary<int, (NetworkModuleSpecification spec, NetworkModule module)> _moduleLookup;

        public NetworkTopologyBuilder(NetworkSpecification specification, IWorkOrchestrator workOrchestrator = null)
        {
            _specification = specification;
            _workOrchestrator = workOrchestrator;
            _moduleLookup = new Dictionary<int, (NetworkModuleSpecification spec, NetworkModule module)>();
        }

        public (INetworkSignalFilter root, INetworkSignalFilter output) CreateConfiguration()
        {
            _specification.Initialise();

            var main = Lookup(_specification.Root.Id);

            var root = new NetworkModule(_specification.InputVectorSize);

            root.Successors.Add(main.mod);
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

            var output = _moduleLookup[_specification.Output.OutputModuleId].module;

            var initd = false;

            for (var i = 0; i < 10; i++)
            {
                if (!Init(root, output))
                {
                    initd = true;
                    break;
                }
            }

            if (!initd)
            {
                throw new ArgumentException(ConfigInvalidMessage);
            }

            return (root, output);
        }

        bool Init(NetworkModule root, NetworkModule output)
        {
            if (!(output is NetworkLayer))
            {
                output.Initialise(_specification.Output.OutputVectorSize);
            }

            bool missing = false;
            var visited = new ConcurrentDictionary<string, int>();

            root.ForwardPropagate(m =>
            {
                var currentMod = (NetworkModule) m;

                visited.AddOrUpdate(m.Id, 1, (id, x) =>
                {
                    DebugOutput.LogVerbose($"Visited {m} ({x})");

                    if (x > 10)
                    {
                        throw new Exception(CircularReferenceMessage);
                    }

                    return x + 1;
                });

                if (!currentMod.IsInitialised)
                {
                    var inputSizes = m.Inputs.Select(x => x.Output.Size).ToArray();

                    missing |= !currentMod.Initialise(inputSizes);

                    if (missing)
                    {
                        DebugOutput.Log(m);
                    }
                }
            });

            return missing;
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

        NetworkModule CreateModuleOrLayer(NetworkModuleSpecification spec)
        {
            if (spec is NetworkLayerSpecification layerSpecification)
            {
                return new NetworkLayer(layerSpecification, _workOrchestrator);
            }

            return new NetworkModule(spec);
        }
    }
}