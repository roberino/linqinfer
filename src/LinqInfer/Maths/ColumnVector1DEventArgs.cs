using System;

namespace LinqInfer.Maths
{
    public class ColumnVector1DEventArgs : EventArgs
    {
        public ColumnVector1DEventArgs(ColumnVector1D vector)
        {
            Vector = vector;
        }

        public ColumnVector1D Vector { get; private set; }
    }
}