using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning
{
    public interface INeuron
    {
        int Size { get; }
        double Bias { get; set; }
        double Output { get; }
        double this[int index] { get; }
        T Calculate<T>(Func<ColumnVector1D, T> func);
        void Adjust(Func<double, double> func);
        void Adjust(ColumnVector1D weightAdjustments);
        double Evaluate(ColumnVector1D input);
        Func<double, double> Activator { get; set; }
    }
}