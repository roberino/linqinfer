using LinqInfer.Data;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    internal interface INetworkSignalFilter : ICloneableObject<INetworkSignalFilter>
    {
        ColumnVector1D Process(ColumnVector1D input);
        INetworkSignalFilter Successor { get; set; }
    }
}
