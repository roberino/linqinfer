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
        private readonly Func<ColumnVector1D> _lossFunction;
        private readonly ColumnVector1D _delta;
        private readonly Softmax _softmax;
        private readonly double _learningRate;

        private ColumnVector1D _previousError;

        public LinearClassifier(int inputVectorSize, int outputVectorSize, double learningRate = 0.1f, double delta = 1)
        {
            Contract.Ensures(inputVectorSize > 0);
            Contract.Ensures(outputVectorSize > 0);
            Contract.Ensures(learningRate > 0);

            _learningRate = learningRate;

            _weights = new Matrix(Enumerable.Range(0, outputVectorSize).Select(n => Functions.RandomVector(inputVectorSize, -0.1, 0.1)));
            _bias = Functions.RandomVector(outputVectorSize);
            _delta = new ColumnVector1D(Vector.UniformVector(_weights.Width, delta));
            _previousError = new ColumnVector1D(Vector.UniformVector(outputVectorSize, 0));
            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights.Width;

        public int OutputVectorSize => _weights.Height;

        public ColumnVector1D Evaluate(IVector input)
        {
            var output = input.Multiply(_weights) + _bias;

            return _softmax.Calculate(output);
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData)
        {
            int i = 0;
            double error = 0;

            foreach (var batch in trainingData.AsQueryable().Chunk())
            {
                var evaluation = batch.Select(b => new
                {
                    score = Evaluate(b.Input),
                    targetOutput = b.TargetOutput
                })
                .Select(s => new
                {
                    score = s.score,
                    targetOutput = s.targetOutput,
                    cost = CalculateCost(s.score, s.targetOutput)
                })
                .Select(e => new
                {
                    score = e.score,
                    targetOutput = e.targetOutput,
                    error = e.cost.Item1,
                    derivative = e.cost.Item2
                })
                .ToList();

                Update(evaluation.Select(c => c.derivative).MeanOfEachDimension());

                error += evaluation.Select(c => c.error).MeanOfEachDimension().Sum();

                i += batch.Count();
            }

            return error / i;
        }

        public double Train(ColumnVector1D input, ColumnVector1D targetOutput)
        {
            var score = Evaluate(input);
            var cost = CalculateCost(score, targetOutput);

            Update(cost.Item2);

            return cost.Item1.Sum();
        }

        private Tuple<ColumnVector1D, ColumnVector1D> CalculateCost(ColumnVector1D score, IVector targetOutput)
        {
            var error = targetOutput.Multiply(score.Log());
            var derivative = (error * (targetOutput.ToColumnVector() - score));

            return new Tuple<ColumnVector1D, ColumnVector1D>(error, derivative);
        }

        private void Update(ColumnVector1D error)
        {
            foreach (var row in _weights)
            {
                row.Apply((w, i) =>
                {
                    return ExecuteUpdateRule(w, error[i], _previousError[i]);
                });
            }
        }

        private double ExecuteUpdateRule(double currentWeightValue, double error, double previousLayerOutput)
        {
            return currentWeightValue + (_learningRate * (currentWeightValue * (error * previousLayerOutput)));
        }
    }
}
