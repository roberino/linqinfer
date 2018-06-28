using LinqInfer.Utility.Expressions;
using NUnit.Framework;
using System;
using System.Linq.Expressions;
using LinqInfer.Maths;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void Parse_GivenExpressionWithFractions_CorrectResultReturned()
        {
            var exp = FractionExp(x => x.Value1 + x.Value2);

            var func = exp.ExportAsString().AsExpression<MyParamsWithFractions, Fraction>().Compile();

            var input = new MyParamsWithFractions()
            {
                Value1 = Fraction.ApproxPii,
                Value2 = Fraction.E
            };

            var result = func(input);

            var expected = exp.Compile().Invoke(input);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_GivenExpressionWithVectorOperation_CorrectResultReturned()
        {
            var exp = VectExp(x => x.Input1.ToColumnVector() * x.Input2.ToColumnVector());

            var func = exp.ExportAsString().AsExpression<MyParamsWithVector, IVector>().Compile();

            var input = new MyParamsWithVector()
            {
                Input1 = new ColumnVector1D(1.1, 2.2),
                Input2 = new ColumnVector1D(3.1, 4.2),
            };

            var result = func(input);

            var expected = exp.Compile().Invoke(input);

            // [3.41,9.24]

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_GivenExpressionWithVector_CorrectResultReturned()
        {
            var exp = VectExp(x => x.Input1.MultiplyBy(x.Input2));

            var func = exp.ExportAsString().AsExpression<MyParamsWithVector, IVector>().Compile();

            var input = new MyParamsWithVector()
            {
                Input1 = new ColumnVector1D(1.1, 2.2),
                Input2 = new ColumnVector1D(3.1, 4.2),
            };

            var result = func(input);

            var expected = input.Input1.MultiplyBy(input.Input2);

            // [3.41,9.24]

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("x => x.Z * 2 + 5 * 2", 16d)]
        [TestCase("x => x.Z + 1 * x.Z - 2", 4d)]
        public void Parse_GivenNumerousOperators_CreatesCorrectPrecedence(string expression, double expectedResult)
        {
            var func = expression.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {Z = 3, X = 12, Y = -4});

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Parse_GivenExpressionWithFieldAccessor_CorrectResultReturned()
        {
            var exp = Exp(x => x.Field1 * 2);
            
            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {Field1 = 9});

            Assert.That(result, Is.EqualTo(18));
        }

        [Test]
        public void Parse_GivenExpressionWithMathFunction_CorrectResultReturned()
        {
            var exp = Exp(x => Math.Sqrt(x.Z)); // "x => Sqrt(x.Z)"
            
            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {Z = 9});

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void Parse_GivenMultiplyExpressionWithConversion_CorrectResultReturned()
        {
            var exp = Exp(x => x.X * x.Y); // "x => Convert((x.X * x.Y), Double)"
            
            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {X = 2, Y = 3});

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void Parse_GivenAdditionExpression_CorrectResultReturned()
        {
            var exp = Exp(x => x.Z + 2.2d);
            
            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {Z = 1.1});

            Assert.That(Math.Round(result, 5), Is.EqualTo(3.3));
        }

        static Expression<Func<MyParams, double>> Exp(Expression<Func<MyParams, double>> exp) => exp;

        static Expression<Func<MyParamsWithVector, IVector>> VectExp(Expression<Func<MyParamsWithVector, IVector>> exp) => exp;

        static Expression<Func<MyParamsWithFractions, Fraction>> FractionExp(Expression<Func<MyParamsWithFractions, Fraction>> exp) => exp;

        private class MyParams
        {
            public long Field1 { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public double Z { get; set; }
        }

        private class MyParamsWithVector
        {
            public IVector Input1 { get; set; }
            public IVector Input2 { get; set; }
        }

        private class MyParamsWithFractions
        {
            public Fraction Value1 { get; set; }
            public Fraction Value2 { get; set; }
        }
    }
}
