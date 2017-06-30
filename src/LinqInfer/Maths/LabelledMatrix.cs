using System;
using System.Collections.Generic;

namespace LinqInfer.Maths
{
    public sealed class LabelledMatrix<T> : Matrix
        where T : IEquatable<T>
    {
        internal LabelledMatrix(Matrix baseMatrix, IDictionary<T, int> labelIndexes) : base(baseMatrix.Rows)
        {
            LabelIndexes = labelIndexes;
        }

        public IDictionary<T, int> LabelIndexes { get; private set; }
    }
}