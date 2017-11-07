using LinqInfer.Data;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    public interface INetworkSignalFilter : ICloneableObject<INetworkSignalFilter>
    {
        IVector Process(IVector input);
        INetworkSignalFilter Successor { get; set; }
    }
}
