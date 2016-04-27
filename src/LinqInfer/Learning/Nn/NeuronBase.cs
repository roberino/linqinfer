using LinqInfer.Maths;
using System;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    public class NeuronBase : INeuron
    {
        private readonly ColumnVector1D _weights;

        public NeuronBase(int inputVectorSize)
        {
            Bias = 0; // -0.3 + Functions.RandomDouble(0.6);
            _weights = Functions.RandomVector(inputVectorSize, -0.7, 0.7);
        }

        public NeuronBase(int inputVectorSize, Range range)
        {
            Bias = range.Min + Functions.RandomDouble(range.Size);
            _weights = Functions.RandomVector(inputVectorSize, range);
        }

        public NeuronBase(ColumnVector1D weights)
        {
            Bias = -0.3 + Functions.RandomDouble(0.6);
            _weights = weights;
        }

        public Func<double, double> Activator { get; set; }

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

        public virtual T Calculate<T>(Func<ColumnVector1D, T> func)
        {
            return func(_weights);
        }

        public virtual double Evaluate(ColumnVector1D input)
        {
            var sum = Bias + _weights.Zip(input, (w, m) => w * m).Sum();

            return Output = Activator == null ? sum : Activator(sum);
        }
    }
}