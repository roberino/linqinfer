using LinqInfer.Utility;

namespace LinqInfer.Maths
{
    public class MinMaxVector
    {
        public MinMaxVector(IVector min, IVector max)
        {
            Min = ArgAssert.AssertNonNull(min, nameof(min));
            Max = ArgAssert.AssertNonNull(max, nameof(max));

            ArgAssert.Assert(() => min.Size == max.Size, "Inconsistent sized vectors");
        }

        public IVector Min { get; }

        public IVector Max { get; }
    }
}