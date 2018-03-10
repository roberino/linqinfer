using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface ILossFunction
    {
        ErrorAndLossVectors Calculate(IVector actualOutput, IVector targetOutput, Func<double, double> derivative);
    }
}