using LinqInfer.Utility;

namespace LinqInfer.Maths
{
    class VectorPool : ObjectPool<ColumnVector1D>
    {
        public VectorPool(int vectorSize, int initialBufferSize = 64) : base(initialBufferSize, 
                (p) =>
                {
                    var v = new ColumnVector1D(new double[vectorSize]);

                    v.Disposing += (s, e) =>
                    {
                        p.Reuse((ColumnVector1D)s);
                    };

                    return v;
                })
        {
        }
    }
}