using LinqInfer.Data;
using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification
{
    public interface INeuron : ICloneableObject<INeuron>
    {
        int Size { get; }
        double Bias { get; }
        double Output { get; }
        double this[int index] { get; }
        void Adjust(Func<double, int, double> func);
        double Evaluate(IVector input);
        Func<double, double> Activator { get; set; }
        void PruneWeights(params int[] indexes);
        ColumnVector1D Export();
        void Import(ColumnVector1D data);
    }
}