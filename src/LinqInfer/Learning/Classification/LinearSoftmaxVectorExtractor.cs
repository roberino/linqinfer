using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public sealed class LinearSoftmaxVectorExtractor : IVectorClassifier
    {
        private readonly Matrix _weights0;
        private readonly Matrix _weights1;
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

            // W = n features * n classes
            _weights0 = new Matrix(Enumerable.Range(0, hiddenLayerSize).Select(n => Functions.RandomVector(inputVectorSize, -0.01, 0.01)));
            _weights1 = new Matrix(Enumerable.Range(0, outputVectorSize).Select(n => Functions.RandomVector(hiddenLayerSize, -0.01, 0.01)));
            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights0.Width;

        public int OutputVectorSize => _weights1.Height;

        public IVector Evaluate(IVector input)
        {
            var v1 = input.MultiplyBy(_weights0);
            var v2 = v1.MultiplyBy(_weights1);

            return _softmax.Apply(v2);
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _learningRate = rateAdjustment(_learningRate);
        }
    }
}