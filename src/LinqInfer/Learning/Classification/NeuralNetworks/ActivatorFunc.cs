using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IActivatorFunction : IEquatable<IActivatorFunction>
    {
        string Name { get; }
        double Parameter { get; }
        Func<double, double> Activator { get; }
        Func<double, double> Derivative { get; }
        Func<double, IActivatorFunction> Create { get; }
    }

    internal class ActivatorFunc : IActivatorFunction
    {
        private Func<double, double> _activator;
     
        private Func<double, double> _derivative;
     
        private Func<double, IActivatorFunction> _recreate;

        public string Name { get; set; }
        public double Parameter { get; set; }
        public Func<double, double> Activator { get { return _activator; } set { _activator = value; } }
        public Func<double, double> Derivative { get { return _derivative; } set { _derivative = value; } }
        public Func<double, IActivatorFunction> Create { get { return _recreate; } set { _recreate = value; } }

        public override string ToString()
        {
            return Name + "(" + Parameter + ")";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IActivatorFunction);
        }

        public bool Equals(IActivatorFunction other)
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