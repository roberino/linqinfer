using System;

namespace LinqInfer.Learning.Nn
{
    [Serializable]
    public class ActivatorFunc
    {
        [NonSerialized]
        private Func<double, double> _activator;
        [NonSerialized]
        private Func<double, double> _derivative;
        [NonSerialized]
        private Func<double, ActivatorFunc> _recreate;

        public string Name { get; set; }
        public double Parameter { get; set; }
        public Func<double, double> Activator { get { return _activator; } set { _activator = value; } }
        public Func<double, double> Derivative { get { return _derivative; } set { _derivative = value; } }
        public Func<double, ActivatorFunc> Create { get { return _recreate; } set { _recreate = value; } }

        public override string ToString()
        {
            return Name + "(" + Parameter + ")";
        }
    }
}