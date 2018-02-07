using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class LabelledMatrix<T> : Matrix
        where T : IEquatable<T>
    {
        internal LabelledMatrix(Matrix baseMatrix, IDictionary<T, int> labelIndexes) : base(baseMatrix.Rows.Select(r => r.ToColumnVector()))
        {
            LabelIndexes = labelIndexes;
        }

        public IDictionary<T, int> LabelIndexes { get; private set; }
    }
}