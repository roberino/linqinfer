using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public sealed class LinearClassifier : IAssistedLearningProcessor
    {
        private readonly Matrix _weights;
        private readonly ColumnVector1D _bias;
        private readonly Softmax _softmax;
        private readonly double _weightDecay = 0.001;

        private double _learningRate;
        
        public LinearClassifier(int inputVectorSize, int outputVectorSize, double learningRate = 0.1f, double decay = 0.001)
        {
            Contract.Ensures(inputVectorSize > 0);
            Contract.Ensures(outputVectorSize > 0);
            Contract.Ensures(learningRate > 0);

            _learningRate = learningRate;
            _weightDecay = decay;

            // W = n features * n classes
            _weights = new Matrix(Enumerable.Range(0, outputVectorSize).Select(n => Functions.RandomVector(inputVectorSize, -0.01, 0.01)));
            _bias = new ColumnVector1D(Vector.UniformVector(outputVectorSize, 0));
            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights.Width;

        public int OutputVectorSize => _weights.Height;

        public Matrix Vectors => new Matrix(_weights.Transpose().Concat(new[] { _bias })).Transpose();

        public ColumnVector1D Evaluate(IVector input)
        {
            var output = input.Multiply(_weights);

            var biasAdjusted = output + _bias;

            return _softmax.Calculate(output);
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _learningRate = rateAdjustment(_learningRate);
        }

        public double Train(IVector inputVector, IVector output)
        {
            return Train(new[] { new TrainingPair<IVector, IVector>(inputVector, output) }, (n, e) => false);
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, double minError, int maxIterations = 10000)
        {
            return Train(trainingData, (n, e) => n >= maxIterations || e < minError);
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction)
        {
            var i = 0;
            var err = 0d;

            do
            {
                err = Train(trainingData);

                DebugOutput.Log($"{i}: error={err}");

                i++;
            }
            while (!haltingFunction(i, err));

            return err;
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData)
        {
            int i = 0;
            ColumnVector1D error = new ColumnVector1D(Vector.UniformVector(OutputVectorSize, 0));

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
                .ToList();
                
                var dW = Mean(evaluation.Select(c => c.cost.dW)).Transpose();
                var dB = evaluation.Select(c => c.cost.dB).Average();

                var decay = (_weightDecay / 2) * _weights.Sum(v => v.DotProduct(v));

                Update(dW, dB, decay);

                error += evaluation.Select(c => c.cost.Cost.ToColumnVector()).MeanOfEachDimension() + decay;

                i += batch.Count();
            }

            return (error / i).EuclideanLength;
        }

        private Matrix Mean(IEnumerable<Matrix> values)
        {
            var h = values.First().Height;

            var rows = Enumerable.Range(0, h).Select(i => values.Select(m => new ColumnVector1D(m.Rows[i])).MeanOfEachDimension());

            return new Matrix(rows);
        }

        private LossAndDerivative CalculateCostAndDerivative(IVector input, ColumnVector1D score, IVector targetOutput)
        {
            var cost = targetOutput.Multiply(score.Log()).ToColumnVector();

            var grad = targetOutput.ToColumnVector() - score;

            var dW = new Matrix(input.ToColumnVector().Select(x => grad * x));

            var dB = grad.Sum();

            return new LossAndDerivative()
            {
                Cost = cost,
                Gradient = grad,
                dW = dW,
                dB = dB
            };
        }

        private void Update(Matrix dW, double dB, double decay)
        {
            ArgAssert.AssertEquals(dW.Width, _weights.Width, nameof(dW.Width));

            foreach (var row in _weights.Zip(dW, (w, u) => new { w = w, u = u }))
            {
                row.w.Apply((w, i) =>
                {
                    return ExecuteUpdateRule(w, -row.u[i], decay);
                });
            }

            _bias.Apply(w => ExecuteUpdateRule(w, dB));
        }

        private double ExecuteUpdateRule(double currentWeightValue, double adjustment, double decay = 0)
        {
            adjustment = adjustment + (adjustment * decay);

            return currentWeightValue + (-_learningRate * adjustment);
        }

        private class LossAndDerivative
        {
            public ColumnVector1D Gradient;
            public IVector Cost;
            public Matrix dW;
            public double dB;
        }
    }
}