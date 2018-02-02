using System;

namespace LinqInfer.Learning.Classification
{
    public class ActivatorFunc : IEquatable<ActivatorFunc>
    {
        private Func<double, double> _activator;
     
        private Func<double, double> _derivative;
     
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

        public override bool Equals(object obj)
        {
            return Equals(obj as ActivatorFunc);
        }

        public bool Equals(ActivatorFunc other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Name, other.Name) && Parameter == other.Parameter;
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode()).GetValueOrDefault() * Parameter.GetHashCode();
        }
    }
}