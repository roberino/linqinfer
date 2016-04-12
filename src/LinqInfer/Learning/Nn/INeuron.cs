using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning
{
    public interface INeuron
    {
        int Size { get; }
        void Adjust(Func<double, double> func);
        double Calculate(ColumnVector1D input);
    }
}