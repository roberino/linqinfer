using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface ILossFunction
    {
        ErrorAndLossVectors Calculate(IVector actualOutput, IVector targetOutput);
        ErrorAndLoss Calculate(double actualOutput, double targetOutput);
    }
}