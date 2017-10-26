using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public sealed class LinearClassifier
    {
        private readonly Matrix _weights;
        private readonly ColumnVector1D _bias;
        private readonly ColumnVector1D _delta;
        private readonly Softmax _softmax;
        private readonly double _weightDecay = 0.001;
        private readonly double _learningRate;
        
        public LinearClassifier(int inputVectorSize, int outputVectorSize, double learningRate = 0.05f, double delta = 1)
        {
            Contract.Ensures(inputVectorSize > 0);
            Contract.Ensures(outputVectorSize > 0);
            Contract.Ensures(learningRate > 0);

            _learningRate = learningRate;

            _weights = new Matrix(Enumerable.Range(0, outputVectorSize).Select(n => Functions.RandomVector(inputVectorSize, -0.01, 0.01)));
            _bias = new ColumnVector1D(Vector.UniformVector(outputVectorSize, 0));
            _delta = new ColumnVector1D(Vector.UniformVector(_weights.Width, delta));
            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights.Width;

        public int OutputVectorSize => _weights.Height;

        public ColumnVector1D Evaluate(IVector input)
        {
            var output = input.Multiply(_weights) + _bias;

            return _softmax.Calculate(output);
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction)
        {
            var i = 0;
            var err = 0d;

            do
            {
                err = Train(trainingData);
                i++;
            }
            while (!haltingFunction(i, err));

            return err;
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData)
        {
            int i = 0;
            double error = 0;

            foreach (var batch in trainingData.AsQueryable().Chunk())
            {
                var evaluation = batch.Select(b => new
                {
                    input = b.Input,
                    score = Evaluate(b.Input),
                    targetOutput = b.TargetOutput
                })
                .Select(s => new
                {
                    score = s.score,
                    targetOutput = s.targetOutput,
                    cost = CalculateCostAndDerivative(s.input, s.score, s.targetOutput)
                })
                .Select(e => new
                {
                    score = e.score,
                    targetOutput = e.targetOutput,
                    error = e.cost.Cost,
                    derivative = e.cost.dW,
                    db = e.cost.dB
                })
                .ToList();

                var dW = evaluation.Select(c => c.derivative.ToColumnVector()).MeanOfEachDimension().Negate();

                Update(dW, -evaluation.Average(c => c.db));

                error += evaluation.Select(c => c.error).Average();

                i += batch.Count();
            }

            return error / i;
        }

        public double Train(ColumnVector1D input, ColumnVector1D targetOutput)
        {
            var score = Evaluate(input);
            var lossAndD = CalculateCostAndDerivative(input, score, targetOutput);

            Update(lossAndD.dW, lossAndD.dB);

            return lossAndD.Cost + lossAndD.Decay;
        }

        private LossAndDerivative CalculateCostAndDerivative(IVector input, ColumnVector1D score, IVector targetOutput)
        {
            var cost = -targetOutput.DotProduct(score.Log());

            var decay = (_weightDecay / 2) * _weights.Sum(v => v.DotProduct(v));

            var grad = score - targetOutput.ToColumnVector();
            var dW = input.Multiply(grad).ToColumnVector();
            var dB = grad.Sum();

            if (_weightDecay != 1) dW.Apply(d => d * _weightDecay);

            return new LossAndDerivative()
            {
                Cost = cost,
                Decay = decay,
                dW = dW,
                dB = dB
            };
        }

        private class LossAndDerivative
        {
            public double Cost;
            public double Decay;
            public IVector dW;
            public double dB;
        }

        private void Update(IVector dW, double dB)
        {
            foreach (var row in _weights)
            {
                row.Apply((w, i) =>
                {
                    return ExecuteUpdateRule(w, dW[i]);
                });
            }

            _bias.Apply(w => ExecuteUpdateRule(w, dB));
        }

        private double ExecuteUpdateRule(double currentWeightValue, double adjustment)
        {
            return currentWeightValue + (-_learningRate * adjustment);
        }
    }
}