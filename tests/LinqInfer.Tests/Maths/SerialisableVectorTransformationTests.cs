using LinqInfer.Maths;
using LinqInfer.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class SerialisableVectorTransformationTests
    {
        [Test]
        public void T()
        {
            var transform = new SerialisableVectorTransformation(new Matrix(new[] { new[] { 1d, 3d, 5d }, new[] { 11d, 23d, 99d } }));

            var vector = ColumnVector1D.Create(7, 6, 5);

            var transformed = transform.Apply(vector);

            var data = transform.ToVectorDocument().ToClob();

            var doc = (new BinaryVectorDocument()).FromClob(data);


        }
    }
}