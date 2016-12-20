using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class SingularValueDecompositionTests
    {
        [Test]
        public void Transform_SimpleExample_ReturnsExpectedOutput()
        {
            var m = new Matrix(new[]
            {
                new [] { 2d, 4d},
                new [] { 1d, 3d},
                new [] { 0d, 0d},
                new [] { 0d, 0d}
            });

            var svd = new SingularValueDecomposition();

            var t = svd.Transform(m);
        }
    }
}
