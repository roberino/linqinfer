namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface ILossFunction
    {
        ErrorAndLoss Calculate(double actualOutput, double targetOutput);
    }
}