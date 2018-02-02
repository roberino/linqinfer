using LinqInfer.Maths;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public class NeuronBase : INeuron
    {
        private ColumnVector1D _weights;
        
        private Func<double, double> _activator;

        private NeuronBase(ColumnVector1D weights, Func<double, double> activator, bool firstWeightIsBias = false)
        {
            if (firstWeightIsBias)
            {
                _weights = new ColumnVector1D(weights.Skip(1).ToArray());
                Bias = weights[0];
            }
            else
            {
                _weights = weights;
            }
            _activator = activator;
        }

        public NeuronBase(int inputVectorSize)
        {
            Bias = 0; // -0.3 + Functions.RandomDouble(0.6);
            _weights = Functions.RandomVector(inputVectorSize, -0.7, 0.7);
        }

        public NeuronBase(int inputVectorSize, Range range)
        {
            Bias = 0;
            _weights = Functions.RandomVector(inputVectorSize, range);
        }

        public Func<double, double> Activator { get { return _activator; } set { _activator = value; } }

        public int Size { get { return _weights.Size; } }

        public double Output { get; protected set; }

        public double Bias { get; protected set; }

        public double this[int index]
        {
            get
            {
                return _weights[index];
            }
        }

        public virtual void Adjust(Func<double, int, double> func)
        {
            Bias = func(Bias, -1);

            _weights.Apply(func);
        }

        public virtual double Evaluate(IVector input)
        {
            var sum = Bias + input.DotProduct(_weights);

            return Output = Activator == null ? sum : Activator(sum);
        }

        public void PruneWeights(params int[] indexes)
        {
            _weights = _weights.RemoveValuesAt(indexes);
        }

        public object Clone()
        {
            return Clone(true);
        }

        public INeuron Clone(bool deep)
        {
            var clone = new NeuronBase(_weights.Clone(true), _activator);

            clone.Bias = Bias;
            clone.Output = Output;

            return clone;
        }

        public ColumnVector1D Export()
        {
            return new ColumnVector1D(new[] { Bias }.Concat(_weights).ToArray());
        }

        public void Import(ColumnVector1D data)
        {
            Contract.Assert(data != null);
            Contract.Assert(data.Size == _weights.Size + 1);

            Bias = data[0];

            int i = 1;

            _weights.Apply(w => data[i++]);
        }
    }
}