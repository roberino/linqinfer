using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Genetics
{
    public class MutatableCategoricalParameter<T> : MutatableValueParameter<T>
    {
        private readonly Func<T> _randomPicker;
        private readonly int _possibleValueCount;

        public MutatableCategoricalParameter(T initialValue, ISet<T> categories) : base(initialValue)
        {
            _randomPicker = Functions.RandomPicker(categories);
            _possibleValueCount = categories.Count;
        }

        public double RandomVariability { get; set; } = 0.2d;

        public override bool IsExhausted
        {
            get
            {
                return _values.Select(v => v.Item1).Distinct().Count() == _possibleValueCount;
            }
        }

        public override double? ValueFitnessScoreCovariance
        {
            get
            {
                if (_values.Count <= 1) return new double?();

                var lastValues = new List<double>();

                for (var i = _values.Count - 1; i > _values.Count - BACKLOG_SIZE && i > 0; i--)
                {
                    var v = _values[i];
                    lastValues.Add(v.Item2); // use the variance of the fitness value
                }

                var vect = new Vector(lastValues.ToArray());

                return vect.Variance();
            }
        }

        protected override object MutateValue()
        {
            if (_values.Count > 1 && Functions.RandomDouble() >= RandomVariability)
            {
                var fittest = _values.OrderByDescending(v => v.Item2).Take(2).ToArray();

                return Functions.AorB(fittest[0].Item1, fittest[1].Item1);
            }

            return _randomPicker();
        }
    }
}