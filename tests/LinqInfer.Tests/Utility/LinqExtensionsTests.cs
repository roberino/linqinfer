using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace LinqInfer.Tests.Utility
{
    [TestFixture]
    public class LinqExtensionsTests
    {
        [Test]
        public void All_GivenTruePredicate_ReturnsTrue()
        {
            var data = new[] { 'a', 'b', 'c' };

            var result = data.All((n, x) => n > 0 && n < 4);

            Assert.That(result);
        }

        [Test]
        public void All_GivenFalsePredicate_ReturnsFalse()
        {
            var data = new[] { 'a', 'b', 'c' };

            var result = data.All((n, x) => x == 'a');

            Assert.That(result, Is.False);
        }

        [Test]
        public void Invert_InvertsExpressions()
        {
            Expression<Func<string, bool>> e = x => x.Length > 0;
            var ei = e.Invert();

            Assert.IsTrue(e.Compile()("x"));
            Assert.IsFalse(ei.Compile()("x"));
        }

        [Test]
        public void GetPropertyName_SimpleExpression_ReturnsName()
        {
            var stats = new Tuple<string, string>("a", "b");

            var propName = LinqExtensions.GetPropertyName(() => stats.Item1);

            Assert.That(propName, Is.EqualTo("Item1"));
        }
    }
}
