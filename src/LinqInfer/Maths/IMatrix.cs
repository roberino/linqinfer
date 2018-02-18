using LinqInfer.Utility;

namespace LinqInfer.Maths
{
    public interface IMatrix
    {
        int Height { get; }
        int Width { get; }

        IIndexableEnumerable<IVector> Columns { get; }
        IIndexableEnumerable<IVector> Rows { get; }

        IVector Multiply(IVector c);
    }
}