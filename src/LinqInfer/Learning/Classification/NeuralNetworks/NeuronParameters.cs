using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NeuronParameters
    {
        public NeuronParameters(int size, ActivatorFunc activator, Range initialWeightRange)
        {
            Size = size;
            Activator = activator;
            InitialWeightRange = initialWeightRange;
        }

        public int Size { get; }
        public ActivatorFunc Activator { get; }
        public Range InitialWeightRange { get; }
    }
}