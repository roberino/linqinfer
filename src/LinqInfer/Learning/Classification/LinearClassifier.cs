using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace LinqInfer.Learning.Classification
{
    public class LinearClassifier
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

            _weights = new Matrix(Enumerable.Range(0, inputVectorSize).Select(n => Functions.RandomVector(outputVectorSize, -0.1, 0.1)));
            _bias = Functions.RandomVector(outputVectorSize);
            _delta = new ColumnVector1D(Vector.UniformVector(_weights.Width, delta));
            _previousError = new ColumnVector1D(Vector.UniformVector(outputVectorSize, 0));
            _softmax = new Softmax(outputVectorSize);
        }

        public int InputVectorSize => _weights.Height;

        public int OutputVectorSize => _weights.Width;

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            var output = _weights * input + _bias;

            return _softmax.Calculate(output);
        }

        public double Train(IQueryable<Tuple<ColumnVector1D, ColumnVector1D>> trainingData)
        {
            int i = 0;
            double error = 0;

            foreach (var batch in trainingData.Chunk())
            {
                var err = batch.Select(b => CalculateError(b.Item1, b.Item2)).MeanOfEachDimension();

                Update(err);

                error += err.Sum();

                i += batch.Count();
            }

            return error / i;
        }

        public double Train(ColumnVector1D input, ColumnVector1D output)
        {
            var err = CalculateError(input, output);

            Update(err);

            return err.Sum();
        }

        private ColumnVector1D CalculateError(ColumnVector1D input, ColumnVector1D output)
        {
            var score = Evaluate(input);

            var error = (input * (output - score));

            error.Apply(x => Math.Max(x, 0));

            return error;
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

        protected virtual double ExecuteUpdateRule(double currentWeightValue, double error, double previousLayerOutput)
        {
            return currentWeightValue + (_learningRate * (currentWeightValue * (error * previousLayerOutput)));
        }
    }
}
