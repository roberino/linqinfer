using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public struct ErrorAndLossVectors
    {
        public double Loss { get; set; }
        public Vector DerivativeError { get; set; }
    }
}