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
        public void Invert_InvertsExpressions()
        {
            Expression<Func<string, bool>> e = x => x.Length > 0;
            var ei = e.Invert();

            Assert.IsTrue(e.Compile()("x"));
            Assert.IsFalse(ei.Compile()("x"));
        }
    }
}
