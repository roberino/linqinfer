using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class PrincipalComponentsAnalysisTests
    {
        [Test]
        public void GetPrincipalFeatureTransformation_ReturnsFeatureReducingFunction()
        {
            var data = new MockFeatures(new[] {
                new [] { 1d, 8, 23, 9, 123},
                new [] { 3d, 5, 24, 10, 123},
                new [] { 2d, 3, 2, 9, 123},
                new [] { 1d, 5, 243, 9, 123}
                });

            var pca = new PrincipalComponentAnalysis(data);

            var transform = pca.CreatePrincipalComponentTransformation(2);

            var x = new[] { 1d, 7, 20, 8, 103.4 };

            var tx = transform(x);

            Assert.That(tx.Length == 2);
        }

        private class MockFeatures : IFeatureDataSource
        {
            private readonly IList<double[]> _data;

            public MockFeatures(IList<double[]> data)
            {
                _data = data;
            }

            public IEnumerable<IFeature> FeatureMetadata
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int VectorSize
            {
                get
                {
                    return _data.First().Length;
                }
            }

            public int SampleCount => _data.Count;

            public IEnumerable<ColumnVector1D> ExtractVectors()
            {
                return _data.Select(d => new ColumnVector1D(d));
            }

            IEnumerable<IVector> IFeatureDataSource.ExtractVectors()
            {
                return ExtractVectors();
            }
        }
    }
}
