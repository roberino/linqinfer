using LinqInfer.Utility.Expressions;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void Parse_GivenExpressionMathFunction_CorrectResultReturned()
        {
            var exp = Exp(x => Math.Sqrt(x.Z)); // "x => Sqrt(x.Z)"
            var parser = new ExpressionParser<MyParams>();
            var exp2 = parser.Parse(exp.ToString());

            var func = exp2.Compile();

            var result = func(new MyParams() {Z = 9});

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void Parse_GivenMultiplyExpressionWithConversion_CorrectResultReturned()
        {
            var exp = Exp(x => x.X * x.Y); // "x => Convert((x.X * x.Y), Double)"
            var parser = new ExpressionParser<MyParams>();
            var exp2 = parser.Parse(exp.ToString());

            var func = exp2.Compile();

            var result = func(new MyParams() {X = 2, Y = 3});

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void Parse_GivenAdditionExpression_CorrectResultReturned()
        {
            var exp = Exp(x => x.Z + 2.2d);
            var parser = new ExpressionParser<MyParams>();
            var exp2 = parser.Parse(exp.ToString());

            var func = exp2.Compile();

            var result = func(new MyParams() {Z = 1.1});

            Assert.That(Math.Round(result, 5), Is.EqualTo(3.3));
        }

        static Expression<Func<MyParams, double>> Exp(Expression<Func<MyParams, double>> exp) => exp;

        private class MyParams
        {
            public int X { get; set; }

            public int Y { get; set; }

            public double Z { get; set; }
        }
    }
}
