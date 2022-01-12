using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    static class VectorOperations
    {
        public static Func<IEnumerable<IVector>, IVector> CreateOperation(this VectorAggregationType operation)
        {
            switch (operation)
            {
                case VectorAggregationType.Multiply:
                    return VectorFunctions.Product;
                case VectorAggregationType.Add:
                    return VectorFunctions.Sum;
                case VectorAggregationType.Concatinate:
                    return VectorFunctions.Concat;
                case VectorAggregationType.HyperbolicTangent:
                    return HTan;
                case VectorAggregationType.None:
                    return v => v.Single();
                default:
                    throw new NotSupportedException(operation.ToString());
            }
        }

        static IVector HTan(this IEnumerable<IVector> vectors)
        {
            var htan = Activators.HyperbolicTangent();
            return new ColumnVector1D(vectors
                .SelectMany(v =>
                    v.ToColumnVector()
                        .Select(x => htan.Activator(x))).ToArray());
        }
    }
}
