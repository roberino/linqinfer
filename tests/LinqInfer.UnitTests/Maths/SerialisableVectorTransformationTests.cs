﻿using LinqInfer.Data;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class SerialisableVectorTransformationTests
    {
        [Test]
        public void Transform_Export_ThenImport_CompareTwoInstanceBehaveTheSame()
        {
            var transform = new SerialisableVectorTransformation(new Matrix(new[] { new[] { 1d, 3d, 5d }, new[] { 11d, 23d, 99d } }));

            var vector = ColumnVector1D.Create(7, 6, 5);

            var transformed = transform.Apply(vector);

            var data = transform.ToVectorDocument().ToClob();

            var doc = (new BinaryVectorDocument()).FromClob(data);

            var transform2 = SerialisableVectorTransformation.LoadFromDocument(doc);
            
            var transformed2 = transform2.Apply(vector);

            Assert.That(transformed.Equals(transformed2));
        }

        [Test]
        public void Equals_WhenGivenEquivInstances_ThenReturnsTrue()
        {
            var transform1 = new SerialisableVectorTransformation(new Matrix(new[] { new[] { 1d, 3d, 5d }, new[] { 11d, 23d, 99d } }));
            var transform2 = new SerialisableVectorTransformation(new Matrix(new[] { new[] { 1d, 3d, 5d }, new[] { 11d, 23d, 99d } }));
            var transform3 = transform1;

            Assert.True(transform1.Equals(transform2));
            Assert.True(transform3.Equals(transform2));
        }
    }
}