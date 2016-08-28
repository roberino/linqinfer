using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface ILayer : INetworkSignalFilter, ICloneableObject<ILayer>
    {
        int Size { get; }
        INeuron this[int index] { get; }
        IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func);
        ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func);
        void Grow(int numberOfNewNeurons = 1);
        void Prune(Func<INeuron, bool> predicate);
        BinaryVectorDocument Export();
        void Import(BinaryVectorDocument data);
    }
}