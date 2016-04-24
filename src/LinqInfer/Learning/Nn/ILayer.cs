using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Nn
{
    internal interface ILayer : INetworkSignalFilter
    {
        int Size { get; }
        INeuron this[int index] { get; }
        IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func);

        ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func);
    }
}