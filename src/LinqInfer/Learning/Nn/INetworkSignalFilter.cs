using LinqInfer.Maths;

namespace LinqInfer.Learning.Nn
{
    internal interface INetworkSignalFilter
    {
        ColumnVector1D Process(ColumnVector1D input);
        INetworkSignalFilter Successor { get; set; }
    }
}
