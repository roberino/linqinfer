using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Genetics
{
    public class MutatableDoubleParameter : MutatableParameter
    {
        private readonly List<Tuple<double, double>> _values;
        private readonly double _randomVar;
        private readonly double _initialValue;

        public MutatableDoubleParameter(double initialValue, double minValue, double maxValue)
        {
            Contract.Assert(initialValue >= minValue);
            Contract.Assert(initialValue <= maxValue);
            Contract.Assert(minValue <= maxValue);

            _randomVar = (maxValue - minValue) / 100;
            _values = new List<Tuple<double, double>>();

            CurrentValue = _initialValue = initialValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public static implicit operator double(MutatableDoubleParameter p)
        {
            return (double)p.CurrentValue;
        }

        public static implicit operator float(MutatableDoubleParameter p)
        {
            return (float)(double)p.CurrentValue;
        }

        public override object OptimalValue
        {
            get
            {
                return _values.Count > 0 ? _values.OrderByDescending(v => v.Item2).First().Item1 : CurrentValue;
            }
        }

        public override double? ValueFitnessScoreCovariance
        {
            get
            {
                if (_values.Count <= 1) return new double?();

                var lastValues = new List<double[]>();

                for (var i = _values.Count - 1; i > _values.Count - 50 && i > 0; i--)
                {
                    var v = _values[i];
                    lastValues.Add(new[] { v.Item1, v.Item2 });
                }

                var matrix = new Matrix(lastValues);

                return matrix.CovarianceMatrix[0, 1];
            }
        }

        public override TypeCode Type { get { return TypeCode.Double; } }

        public double MinValue { get; private set; }

        public double MaxValue { get; private set; }

        protected override void SubmitScore(double fitnessScore)
        {
            _values.Add(new Tuple<double, double>((double)CurrentValue, fitnessScore));
        }

        public override void Reset()
        {
            base.Reset();
            _values.Clear();
            CurrentValue = _initialValue;
        }

        protected override object MutateValue()
        {
            if (_values.Count > 0)
            {
                var fittest = _values.OrderByDescending(v => v.Item2).Take(2).ToArray();

                var covar = ValueFitnessScoreCovariance;

                if (covar.GetValueOrDefault(0) != 0)
                {
                    return MaxMinComply(Functions.Mutate(fittest[0].Item1, covar.Value > 0 ? MaxValue : MinValue, _randomVar));
                }

                if (_values.Count > 1)
                {
                    return MaxMinComply(Functions.Mutate(fittest[0].Item1, fittest[1].Item1, _randomVar));
                }
                else
                {
                    return MaxMinComply(Functions.Mutate(fittest[0].Item1, MaxValue, _randomVar));
                }
            }

            return MaxMinComply(Functions.Mutate((double)CurrentValue, MaxValue, _randomVar));
        }

        private double MaxMinComply(double value)
        {
            if (value < MinValue) return MinValue;
            if (value > MaxValue) return MaxValue;
            return value;
        }
    }
}