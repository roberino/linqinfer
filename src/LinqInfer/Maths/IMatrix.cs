using LinqInfer.Utility;

namespace LinqInfer.Maths
{
    public interface IMatrix
    {
        int Height { get; }
        int Width { get; }

        IIndexedEnumerable<IVector> Columns { get; }
        IIndexedEnumerable<IVector> Rows { get; }

        IVector Multiply(IVector c);
    }
}