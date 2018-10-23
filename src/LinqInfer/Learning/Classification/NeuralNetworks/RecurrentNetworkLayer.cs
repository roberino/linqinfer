using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class RecurrentNetworkLayer : NetworkLayer
    {
        readonly VectorBuffer _memory;

        public RecurrentNetworkLayer(
            int inputVectorSize,
            int memorySize,
            ILayer memoryLayer,
            NetworkLayerSpecification specification) 
                : base(inputVectorSize, specification, 
                      new NeuronCluster(inputVectorSize + memoryLayer.Size, specification.LayerSize, specification.NeuronFactory,
            specification.Activator))
        {
            _memory = new VectorBuffer(memorySize, memoryLayer.Size);
            MemoryLayer = memoryLayer;
        }

        public ILayer MemoryLayer { get; }

        public override IVector Process(IVector input)
        {
            var combinedInputItems = _memory.PopAndRead();

            combinedInputItems.Add(input);

            var combinedInput = new MultiVector(combinedInputItems);

            var output = base.Process(combinedInput);

            var memOutput = MemoryLayer.Process(output);

            _memory.Push(memOutput);

            return output;
        }
    }
}