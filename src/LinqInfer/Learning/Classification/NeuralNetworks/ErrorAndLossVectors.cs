using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public struct ErrorAndLossVectors
    {
        public Vector Loss { get; set; }
        public Vector DerivativeError { get; set; }
    }
}