using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal sealed class LinearSoftmaxVectorExtractor : IVectorClassifier
    {
        private readonly Matrix _weights0;
        private readonly ColumnVector1D _bias0;
        private readonly Matrix _weights1;
        private readonly ColumnVector1D _bias1;
        private readonly Softmax _softmax;
        private readonly double _weightDecay = 0.001;

        private double _learningRate;

        public LinearSoftmaxVectorExtractor(
            int inputVectorSize, 
            int outputVectorSize, 
            int hiddenLayerSize,
            double learningRate = 0.1f, 
            double decay = 0.001)
        {
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));
            ArgAssert.AssertGreaterThanZero(hiddenLayerSize, nameof(hiddenLayerSize));
            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));
            ArgAssert.AssertGreaterThanZero(learningRate, nameof(learningRate));

            _learningRate = learningRate;
            _weightDecay = decay;

            _weights0 = Matrix.RandomMatrix(hiddenLayerSize, inputVectorSize, new Range(0.01, -0.01));
            _weights1 = Matrix.RandomMatrix(outputVectorSize, hiddenLayerSize, new Range(0.01, -0.01));

            _bias0 = new ColumnVector1D(Vector.UniformVector(hiddenLayerSize, 0));
            _bias1 = new ColumnVector1D(Vector.UniformVector(outputVectorSize, 0));

            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights0.Width;

        public int OutputVectorSize => _weights1.Height;

        public IEnumerable<Matrix> Weights
        {
            get
            {
                yield return _weights0;
                yield return _weights1;
            }
        }

        public IVector Evaluate(IVector input)
        {
            return EvaluateInternal(input).Last();
        }

        public double Train(IVector input, IVector targetOutput)
        {
            var lAndD = CalculateLossAndDerivative(input, targetOutput);

            UpdateWeights1(lAndD.Gradient, lAndD.HiddenOutput);
            UpdateWeights0(lAndD);

            return -lAndD.Loss.Sum;
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _learningRate = rateAdjustment(_learningRate);
        }

        public double CalculateError(IVector input, IVector targetOutput)
        {
            return -CalculateLossAndDerivative(input, targetOutput).Loss.Sum;
        }

        private IVector[] EvaluateInternal(IVector input)
        {
            var v1 = input.HorizontalMultiply(_weights0).ToColumnVector() + _bias0;
            var v2 = v1.HorizontalMultiply(_weights1).ToColumnVector() + _bias1;

            return new[] { v1, v2, _softmax.Apply(v2) };
        }

        private LossAndDerivative CalculateLossAndDerivative(IVector input, IVector targetOutput)
        {
            var result = EvaluateInternal(input);
            var actualOutput = result.Last();

            var loss = targetOutput.MultiplyBy(actualOutput.ToColumnVector().Log()).ToColumnVector();

            var error = actualOutput.ToColumnVector() - targetOutput.ToColumnVector();

            var dW = new Matrix(input.ToColumnVector().Select(x => error * x));

            return new LossAndDerivative()
            {
                Loss = loss,
                Gradient = error,
                dW = dW,
                HiddenOutput = result.First()
            };
        }

        private void UpdateWeights1(IVector error, IVector hiddenOutput)
        {
            // j = cols
            // i = rows

            // w1[i, j] (new) = w[i, j] (old) - n * e[j] * h[i]

            _weights1.Apply((j, i, w) =>
                w - _learningRate * error[j] * hiddenOutput[i]                
                );

            _bias1.Apply((w, j) => w - _learningRate * error[j]);
        }

        private void UpdateWeights0(LossAndDerivative ld)
        {
            var dW = ld.dW;
            var error = ld.Gradient;

            dW.Apply((r, c, x) => x * _learningRate);

            _weights0.Overwrite(_weights0 - dW);

            _bias0.Apply((w, j) => w - _learningRate * error[j]);
        }

        private class LossAndDerivative
        {
            public ColumnVector1D Gradient;
            public IVector HiddenOutput;
            public IVector Loss;
            public Matrix dW;
        }
    }
}