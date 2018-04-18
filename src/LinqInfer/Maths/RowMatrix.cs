using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    internal class RowMatrix : IMatrix
    {
        private readonly IVector _row;

        internal RowMatrix(IVector row)
        {
            _row = row;
            Rows = new IndexableEnumerable<IVector>(new List<IVector> { row });
        }

        public int Height => 1;

        public int Width => _row.Size;

        public IIndexedEnumerable<IVector> Columns => new IndexableEnumerable<IVector>(_row.ToColumnVector().GetUnderlyingArray().Select(v => new ColumnVector1D(v)));

        public IIndexedEnumerable<IVector> Rows { get; }

        public IVector Multiply(IVector c)
        {
            // a, b, c * x
            //           y
            //           z

            ArgAssert.AssertEquals(_row.Size, c.Size, nameof(c.Size));

            return new ColumnVector1D(_row.DotProduct(c));
        }
    }
}