using LinqInfer.Maths;
using System;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    public class NeuronBase : INeuron
    {
        private ColumnVector1D _weights;

        public NeuronBase(int inputVectorSize)
        {
            _weights = Functions.RandomVector(inputVectorSize);
        }

        public NeuronBase(int inputVectorSize, Range range)
        {
            _weights = Functions.RandomVector(inputVectorSize, range);
        }

        public NeuronBase(ColumnVector1D weights)
        {
            _weights = weights;
        }

        public int Size { get { return _weights.Size; } }

        public double Output { get; private set; }

        public Func<double, double> Activator { get; set; }

        public double Bias { get; set; }

        public double this[int index]
        {
            get
            {
                return _weights[index];
            }
        }

        public virtual void Adjust(Func<double, double> func)
        {
            _weights.Apply(func);
        }

        public virtual T Calculate<T>(Func<ColumnVector1D, T> func)
        {
            return func(_weights);
        }

        public virtual double Evaluate(ColumnVector1D input)
        {
            var sum = Bias + _weights.Zip(input, (w, m) => w * m).Sum();

            return Output = Activator == null ? sum : Activator(sum);
        }

        public void Adjust(ColumnVector1D weightAdjustments)
        {
            _weights += weightAdjustments;
        }
    }
}