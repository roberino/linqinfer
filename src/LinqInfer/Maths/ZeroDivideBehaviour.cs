using System;

namespace LinqInfer.Maths
{
    public enum ZeroDivideBehaviour
    {
        ReturnNan,
        ReturnZero,
        ReturnOne,
        ReturnZeroOrOne,
        ThrowError
    }

    public static class ZeroDivideBehaviourExtensions
    {
        public static Func<double, double, double> CreateDivider(this ZeroDivideBehaviour behaviour)
        {
            switch (behaviour)
            {
                case ZeroDivideBehaviour.ReturnOne:
                    return (x, y) =>
                    {
                        if (y == 0) return 1;
                        return x / y;
                    };
                case ZeroDivideBehaviour.ReturnZero:
                    return (x, y) =>
                    {
                        if (y == 0) return 0;
                        return x / y;
                    };
                case ZeroDivideBehaviour.ThrowError:
                    return (x, y) =>
                    {
                        if (y == 0) throw new DivideByZeroException();
                        return x / y;
                    };
            }

            return (x, y) => x / y;
        }
    }
}