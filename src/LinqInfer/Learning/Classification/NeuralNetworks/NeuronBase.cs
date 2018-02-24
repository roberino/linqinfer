using LinqInfer.Maths;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public class NeuronBase : INeuron
    {
        private ColumnVector1D _weights;

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

            Activator = activator;
        }

        public NeuronBase(int inputVectorSize)
        {
            Bias = 0;
            _weights = Functions.RandomVector(inputVectorSize, -0.7, 0.7);
        }

        public NeuronBase(int inputVectorSize, Range range)
        {
            Bias = 0;
            _weights = range.Size == 0 ?
                Vector.UniformVector(inputVectorSize, 0).ToColumnVector() :
                Functions.RandomVector(inputVectorSize, range);
        }

        public double this[int index] => _weights[index];

        public int Size => _weights.Size;

        public Func<double, double> Activator { get; set; }

        public double Output { get; protected set; }

        public double Bias { get; protected set; }

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

        public object Clone() => Clone(true);

        public INeuron Clone(bool deep)
        {
            return new NeuronBase(_weights.Clone(true), Activator)
            {
                Bias = Bias,
                Output = Output
            };
        }

        public ColumnVector1D Export() => new ColumnVector1D(new[] { Bias }.Concat(_weights).ToArray());
        
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