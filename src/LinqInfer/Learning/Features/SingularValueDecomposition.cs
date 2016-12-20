using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    internal class SingularValueDecomposition
    {
        public Matrix Transform(Matrix a)
        {
            var at = a.Transpose();
            var aat = a * at;
            var ata = at * a;

            var uEVD = new EigenvalueDecomposition(aat);
            var vEVD = new EigenvalueDecomposition(ata);

            throw new NotImplementedException();
        }
    }
}