using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    internal interface ILayer : INetworkSignalFilter, ICloneableObject<ILayer>
    {
        int Size { get; }
        INeuron this[int index] { get; }
        IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func);
        ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func);
    }
}