using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Genetics
{
    public abstract class MutatableValueParameter<T> : MutatableParameter
    {
        protected readonly List<Tuple<T, double>> _values;
        protected readonly T _initialValue;

        protected MutatableValueParameter(T initialValue)
        {
            _values = new List<Tuple<T, double>>();
            _currentValue = _initialValue = initialValue;
        }

        public static implicit operator T(MutatableValueParameter<T> p)
        {
            return (T)p.CurrentValue;
        }

        public override object OptimalValue
        {
            get
            {
                return _values.Count > 0 ? _values.OrderByDescending(v => v.Item2).First().Item1 : CurrentValue;
            }
        }

        public override TypeCode Type { get { return System.Type.GetTypeCode(typeof(T)); } }

        protected override void SubmitScore(double fitnessScore)
        {
            _values.Add(new Tuple<T, double>((T)CurrentValue, fitnessScore));
        }

        protected override void OnReset()
        {
            _values.Clear();
            _currentValue = _initialValue;
        }
    }
}