using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    abstract class LossFunctionBase : ILossFunction
    {
        bool? _useOneOfN;

        public NetworkError Calculate(IVector actualOutput, IVector targetOutput, Func<double, double> derivative)
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

        protected abstract NetworkError CalculateNormalVector(ColumnVector1D actualOutput, IVector targetOutput, Func<double, double> derivative);

        protected abstract NetworkError CalculateOneOfN(ColumnVector1D actualOutput, OneOfNVector targetOutput, Func<double, double> derivative);
    }
}
