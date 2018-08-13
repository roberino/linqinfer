using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    abstract class LossFunctionBase : ILossFunction
    {
        bool? _useOneOfN;

        public ErrorAndLossVectors Calculate(IVector actualOutput, IVector targetOutput, Func<double, double> derivative)
        {
            if (!_useOneOfN.HasValue)
            {
                _useOneOfN = targetOutput is OneOfNVector;
            }

            if (_useOneOfN.Value)
            {
                return CalculateOneOfN(actualOutput.ToColumnVector(), (OneOfNVector)targetOutput, derivative);
            }

            return CalculateNormalVector(actualOutput.ToColumnVector(), targetOutput, derivative);
        }

        protected abstract ErrorAndLossVectors CalculateNormalVector(ColumnVector1D actualOutput, IVector targetOutput, Func<double, double> derivative);

        protected abstract ErrorAndLossVectors CalculateOneOfN(ColumnVector1D actualOutput, OneOfNVector targetOutput, Func<double, double> derivative);
    }
}
