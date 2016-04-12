using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Nn
{
    public class NeuronBase : INeuron
    {
        private readonly ColumnVector1D _weights;

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

        public virtual void Adjust(Func<double, double> func)
        {
            _weights.Apply(func);
        }

        public virtual double Calculate(ColumnVector1D input)
        {
            var sum = _weights.Zip(input, (w, m) => w * m).Sum();

            return sum;
        }
    }
}