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
        public void Parse_GivenExpressionWithGrouping_GroupPrecedencyIsPreserved()
        {
            var exp0 = Exp(x => (x.Z + 1) * (x.Z + 2));
            var expression = exp0.ExportAsString();

            var exp = expression.AsExpression<MyParams, double>();

            var paramz = new MyParams() {Z = 14};

            var result = exp.Compile().Invoke(paramz);
            var expected = exp0.Compile().Invoke(paramz);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_GivenExpressionWithMinus_CorrectResultReturned()
        {
            var expression = "_ => 5.1 - -6.2";

            var exp = expression.AsExpression<MyParams, double>();
            
            var result = exp.Compile().Invoke(new MyParams());

            Assert.That(result, Is.EqualTo(5.1 - -6.2));
        }

        [Test]
        public void Parse_GivenExpressionWithClosure_CorrectResultReturned()
        {
            var i = new {x = 1};
            var z = 123;

            var exp = Exp(i, 0, x => x.x + z);

            var func = exp.ExportAsString().AsFunc(i, 0);

            var result = func();

            var expected = exp.Compile().Invoke(i);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_GivenAnonymousParameterExpression_CorrectResultReturned()
        {
            var i = new {x = 1, y = "s"};

            var exp = Exp(i, 0, x => x.x + x.y.Length);

            var func = exp.ExportAsString().AsFunc(i, 0);

            var result = func();

            var expected = exp.Compile().Invoke(i);

            Assert.That(result, Is.EqualTo(expected));
        }

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

            var exp2 = exp.ExportAsString().AsExpression<MyParamsWithVector, IVector>();

            var func = exp2.Compile();

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
        [TestCase("x => x.Z + 1 * (x.Z) - 2", 4d)]
        [TestCase("x => x.Z + ((1 * x.Z)) - 2", 4d)]
        [TestCase("x => (x.Z + 1) * x.Z - 2", 10d)]
        [TestCase("x => ((x.Z + 1) * x.Z) - 2", 10d)]
        public void Parse_GivenNumerousOperators_CreatesCorrectPrecedence(string expression, double expectedResult)
        {
            var func = expression.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams() {Z = 3, X = 12, Y = -4});

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Parse_GivenExpressionWithFieldAccessor_CorrectResultReturned()
        {
            var exp = Exp(x => x.Field1 * (long)2);
            
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
            var exps = exp.ExportAsString();
            
            var func = exps.AsExpression<MyParams, double>().Compile();

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

        static Expression<Func<TInput, TOuput>> Exp<TInput, TOuput>(TInput inputExample, TOuput outputExample,
            Expression<Func<TInput, TOuput>> exp) => exp;

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
