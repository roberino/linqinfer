using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Maths;
using LinqInfer.Utility.Expressions;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void AsExpression_Loop_BehavesCorrectly()
        {
            var exp = "(a) => Loop(x => x * 5, 5)".AsExpression<double, IPromise<int[]>>();

            var result = exp.Compile().Invoke(1).Result;

            Assert.That(result.Length, Is.EqualTo(5));

            var i = 0;

            foreach (var x in result)
            {
                Assert.That(x, Is.EqualTo(5 * i++));
            }
        }

        [Test]
        public void AsExpression_LoopThenFunc_BehavesCorrectly()
        {
            var exp = "(a) => Loop(x => x * 5, 5).Then(result => result[2]).Model".AsExpression<double, int>();

            var result = exp.Compile().Invoke(1);

            Assert.That(result, Is.EqualTo(10));
        }
        
        [Test]
        public void AsExpression_Recurse_BehavesCorrectly()
        {
            ControlFunctions.Recurse<int>((i, x) => (1, true));

            var exp = "(a) => Recurse((i, x : int) => ((x + 1) * 5, x > 1)).Model".AsExpression<double, int[]>();

            var result = exp.Compile().Invoke(1);

            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(5));
            Assert.That(result[1], Is.EqualTo(30));
        }

        [Test]
        public void AsExpression_LoopUntil_BehavesCorrectly()
        {
            var exp = "(a) => LoopUntil(x => x * 5, (x, r) => r <= a || x > 100)".AsExpression<double, IPromise<int[]>>();

            var result = exp.Compile().Invoke(15).Result;

            Assert.That(result.Length, Is.EqualTo(5));

            var i = 0;

            foreach (var x in result)
            {
                Assert.That(x, Is.EqualTo(5 * i++));
            }
        }

        [TestCase("x => x.Z > 0 ? 2.1 : 2.9 + 5", -12, 7.9)]
        [TestCase("x => x.Z > 0 && x.Z > 1 ? 1 : 2", 2, 1)]
        [TestCase("x => x.Z > 1 + 1 ? 2.1 : 5.0", 2, 5)]
        [TestCase("x => true ? 1 : -1", 0, 1)]
        [TestCase("x => (5 > 4) ? 1 : -1", 0, 1)]
        [TestCase("x => Convert((((x.Z > 0.5) ? 1 : 0)), Double)", 1, 1)]
        public void AsExpression_ConditionalExpressions_ParsesCorrectly(string expression, double z, double expected)
        {
            var exp = expression.AsExpression<MyParams, double>();

            var paramz = new MyParams { Z = z };

            var result = exp.Compile().Invoke(paramz);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("x => -(2) * x", 2, -4)]
        [TestCase("x => -2.2 * x", 2, -4.4)]
        [TestCase("x => 5 * x + 4", 2, 14)]
        [TestCase("x => 4 + 5 * x", 2, 14)]
        [TestCase("x => Pow(2.71, 0-(x))", 2.2, 0.11154948255890629)]
        [TestCase("x => Pow(2.71, -(x))", 2.2, 0.11154948255890629)]
        public void AsExpression_NumericExpressions_ParsesCorrectly(string expression, double x, double expected)
        {
            var exp = expression.AsExpression<double, double>();

            var result = exp.Compile().Invoke(x);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ExportAsString_ValueTuple_ReturnTupleExpression()
        {
            var expString = Exp(new int[1], (1, 2), a => ValueTuple.Create(a[0], a[1]))
                .ExportAsString();

            Assert.That(expString, Is.EqualTo("a => (a[0], a[1])"));
        }

        [Test]
        public void ExportAsString_ArrayIndexAccessor_ReturnValidString()
        {
            var expString = Exp(new int[1], 1, a => a[1]).ExportAsString();

            Assert.That(expString, Is.EqualTo("a => a[1]"));
        }

        [Test]
        public void ExportAsString_PropertyIndexAccessor_ReturnValidString()
        {
            var expString = Exp(new ParamsWithIndexedProperty(), 1, a => a[1]).ExportAsString();

            Assert.That(expString, Is.EqualTo("a => a[1]"));
        }

        [Test]
        public void AsExpression_PropertyIndexAccessor_EvaluatesCorrectly()
        {
            var expString = "a => a[2]";

            var exp = expString.AsExpression<ParamsWithIndexedProperty, int>();

            var result = exp.Compile().Invoke(new ParamsWithIndexedProperty());

            Assert.That(result, Is.EqualTo(14));
        }

        [Test]
        public void AsExpression_ConditionWithAddition_EvaluatesCorrectly()
        {
            var exp = "x => x > 1 + 1 ? 10 : 5".AsExpression<double, double>();

            var result = exp.Compile().Invoke(2.2);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void AsExpression_UnknownToken_ReturnsTokenObject()
        {
            var exp = "x => token1".AsExpression<double, Token>();

            var result = exp.Compile().Invoke(2.2);

            Assert.That(result, Is.EqualTo(new Token("token1")));
        }

        [Test]
        public void AsExpression_ArrayIndexer_ReturnsArrayElement()
        {
            var exp = "x => x[2]".AsExpression<double[], double>();

            var result = exp.Compile().Invoke(new[] {1.1d, 2.2d, 3.3d, 4.4d});

            Assert.That(result, Is.EqualTo(3.3d));
        }

        [Test]
        public void AsExpression_ArrayIndexerOnFunc_ReturnsArrayElement()
        {
            var exp = $"x => {nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetPiArray)}(x)[1]".AsExpression<int, double>();

            var result = exp.Compile().Invoke(2);

            Assert.That(result, Is.EqualTo(StaticExampleMethods.GetPiArray(2)[1]));
        }

        [Test]
        public void AsExpression_ArrayIndexerAndPropertyOnFunc_ReturnsArrayElement()
        {
            var exp = $"x => {nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetObjectArray)}(x)[1].Val".AsExpression<int, double>();

            var result = exp.Compile().Invoke(2);

            Assert.That(result, Is.EqualTo(StaticExampleMethods.GetObjectArray(2)[1].Val));
        }
        
        [Test]
        public void AsExpression_StaticMethodFromExternalAsm_BindsCorrectly()
        {
            var exp = $"x => {nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetPiX)}(x)".AsExpression<int, double>();

            var result = exp.Compile().Invoke(5);

            Assert.That(result, Is.EqualTo(StaticExampleMethods.GetPiX(5)));
        }

        [Test]
        public void AsExpression_TupleParameter_BindsCorrectly()
        {
            var exp = $"x => {nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetTupleProduct)}((x, 5.0))".AsExpression<double, int>();

            var result = exp.Compile().Invoke(5);

            Assert.That(result, Is.EqualTo(25));
        }

        [Test]
        public void AsExpression_TaskResult_ReturnsValue()
        {
            var exp = $"x => {nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetValueX15Async)}(x).Model".AsExpression<double, double>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result, Is.EqualTo(45));
        }

        [Test]
        public void AsExpression_TaskResultWithThen_ReturnsValue()
        {
            var methodName = $"{nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetValueX15Async)}";

            var exp = $"x => {methodName}(x).Then(r => r + 1).Model".AsExpression<double, double>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result, Is.EqualTo(46));
        }

        [Test]
        public void AsExpression_Do_CanUseOuterScopedParameter()
        {
            var exp = "x => Do(() => 1 + x).Model".AsExpression<double, double>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void AsExpression_TaskResultWithThenAsync_ReturnsValue()
        {
            var methodName = $"{nameof(StaticExampleMethods)}.{nameof(StaticExampleMethods.GetValueX15Async)}";

            var exp = $"x => {methodName}(x).ThenAsync(r => {methodName}(r)).Model".AsExpression<double, double>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result, Is.EqualTo(15 * 3 * 15));
        }
        
        [Test]
        public void AsExpression_InnerLamdaWithExpression_BindsCorrectly()
        {
            var exp = "x => StaticExampleMethods.GetXOrZero(x, a => a > 1.1)".AsExpression<double, double>();

            var result = exp.Compile().Invoke(5.123d);

            Assert.AreEqual(result, StaticExampleMethods.GetXOrZero(5.123d, a => a > 1.1));
        }

        [Test]
        public void AsExpression_LinqWhere_BindsCorrectly()
        {
            var expStr = Exp(new[] { 1d }, 1d, x => x.Where(n => n > 1).Sum())
                .ExportAsString();

            var exp = expStr.AsExpression<double[], double>();

            var result = exp.Compile().Invoke(new[] {1d, 2d, 3d, 5d, 8d});

            Assert.AreEqual(result, 18d);
        }

        [Test]
        public void AsExpression_InnerLamdaWithExpression2_BindsCorrectly()
        {
            var expStr = Exp(1d, 1d, x => StaticExampleMethods.GetXOrZero(x, a => a > 1.1)).ExportAsString();
            var exp = expStr.AsExpression<double, double>();

            var result = exp.Compile().Invoke(5.123d);

            Assert.AreEqual(result, StaticExampleMethods.GetXOrZero(5.123d, a => a > 1.1));
        }

        [Test]
        public void AsExpression_InnerLambda_BindsCorrectly()
        {
            var exp = $"x => Enumerable.Select(x, a => 1)".AsExpression<IEnumerable<int>, IEnumerable<int>>();

            var result = exp.Compile().Invoke(new[] {1, 2});
        }

        [Test]
        public void AsExpression_StaticLinqMethod_BindsCorrectly()
        {
            var exp = $"x => {nameof(Enumerable)}.{nameof(Enumerable.Range)}(x, 5)".AsExpression<int, IEnumerable<int>>();

            var result = exp.Compile().Invoke(7).ToArray();

            Assert.That(result.Count(), Is.EqualTo(5));
            Assert.That(result.First(), Is.EqualTo(7));
        }

        [Test]
        public void ParseVectorArrayExpression_ReturnsVector()
        {
            var exp = "x => Vector([1.2, 1.1, x])".AsExpression<double, IVector>();

            var result = exp.Compile().Invoke(1.3);

            Assert.That(result[0], Is.EqualTo(1.2));
            Assert.That(result[1], Is.EqualTo(1.1));
            Assert.That(result[2], Is.EqualTo(1.3));
        }

        [Test]
        public void AsExpression_VectorExpression_ReturnsVector()
        {
            var exp = "x => Vector(1.2, 1.1, x)".AsExpression<double, IVector>();

            var result = exp.Compile().Invoke(1.3);

            Assert.That(result[0], Is.EqualTo(1.2));
            Assert.That(result[1], Is.EqualTo(1.1));
            Assert.That(result[2], Is.EqualTo(1.3));
        }

        [Test]
        public void AsExpression_BitVectorExpression_ReturnsBitVector()
        {
            var exp = "x => BitVector(true, false, x) * BitVector(false, false, x)".AsExpression<bool, BitVector>();

            var result = exp.Compile().Invoke(true);

            Assert.That(result.ValueAt(0), Is.False);
            Assert.That(result.ValueAt(1), Is.False);
            Assert.That(result.ValueAt(2), Is.True);
        }

        [Test]
        public void AsExpression_OneOfNVectorExpression_ReturnsOneOfNVector()
        {
            var exp = "x => OneOfNVector(2, x)".AsExpression<int, OneOfNVector>();

            var result = exp.Compile().Invoke(1);

            Assert.That(result.ActiveIndex, Is.EqualTo(1));
            Assert.That(result.Size, Is.EqualTo(2));
        }

        [Test]
        public void AsExpression_OneOfNVectorExpressionWithOutActiveIndex_ReturnsOneOfNVector()
        {
            var exp = "x => OneOfNVector(x)".AsExpression<int, OneOfNVector>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result.ActiveIndex.HasValue, Is.False);
            Assert.That(result.Size, Is.EqualTo(3));
        }

        [Test]
        public void AsExpression_MatrixExpression_ReturnsMatrix()
        {
            var exp = "x => Matrix([[x, 1], [x + 2, 2]])".AsExpression<double, Matrix>();

            var result = exp.Compile().Invoke(3);

            Assert.That(result[0, 0], Is.EqualTo(3));
            Assert.That(result[0, 1], Is.EqualTo(1));
            Assert.That(result[1, 0], Is.EqualTo(5));
            Assert.That(result[1, 1], Is.EqualTo(2));
        }

        [Test]
        public void ExportAsString_NewVector_ReturnsVectorExp()
        {
            var exp = Exp(1d, new Vector(1d, 2d), x => new Vector(x, 2d));
            var expStr = exp.ExportAsString();

            Assert.That(expStr, Is.EqualTo("x => Vector([x, 2.0])"));
        }

        [Test]
        public void AsExpression_InvertedCondition_EvaluatesCorrectly()
        {
            var exp = "x => !x.PF() ? -1 : 2".AsExpression<MyParams, int>();

            var parameter = new MyParams { PFValue = false };

            var result = exp.Compile().Invoke(parameter);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void AsExpression_InvertedConditionWithNumericOperation_EvaluatesCorrectly()
        {
            var exp = "x => 5 + (!x.PF() ? -1 : 2)".AsExpression<MyParams, int>();

            var parameter = new MyParams { PFValue = false };

            var result = exp.Compile().Invoke(parameter);

            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void AsExpression_OperatorsInNonPrecedenceOrder_EvaluatesInCorrectOrder()
        {
            var exp = "x => 4 + 5 * x".AsExpression<double, double>();

            var result = exp.Compile().Invoke(2);

            Assert.That(result, Is.EqualTo(14));
        }

        [Test]
        public void AsExpression_NegatedFunctionParams_ParsesCorrectly()
        {
            var exp = "x => Pow(2.71, 0-(x))".AsExpression<double, double>();

            var result = exp.Compile().Invoke(2.2);

            Assert.That(result, Is.EqualTo(Math.Pow(2.71, -2.2)));
        }

        [TestCase("!x.PF() ? (-1 : 2")]
        public void AsExpression_InvalidExpression_ThrowsCompileError(string expression)
        {
            Assert.Throws<ArgumentException>(() => expression.AsExpression<MyParams, double>());
        }

        [Test]
        public void AsExpression_ThenExport_ReturnsSameExpressionString()
        {
            var expStr = "x => Convert((((x > 0.5) ? 1 : 0)), double)";

            var exp = expStr.AsExpression<double, double>();

            var expStr2 = exp.ExportAsString();

            Assert.That(expStr, Is.EqualTo(expStr2));
        }

        [Test]
        public void AsExpression_Condition_ParsesCorrectly()
        {
            var exp0 = Exp(x => x.Z > 0 ? 1.1 : 2.2);

            var expression = exp0.ExportAsString();

            var exp = expression.AsExpression<MyParams, double>();

            var paramz = new MyParams { Z = 3 };

            var result = exp.Compile().Invoke(paramz);

            Assert.That(result, Is.EqualTo(1.1));
        }

        [Test]
        public void AsExpression_MemberCondition_ParsesCorrectly()
        {
            var exp0 = Exp(x => x.P ? 1.1 : 2.2);

            var expression = exp0.ExportAsString();

            var exp = expression.AsExpression<MyParams, double>();

            var paramz = new MyParams { P = true };

            var result = exp.Compile().Invoke(paramz);

            Assert.That(result, Is.EqualTo(1.1));
        }

        [Test]
        public void AsExpression_Grouping_GroupPrecedencyIsPreserved()
        {
            var exp0 = Exp(x => (x.Z + 1) * (x.Z + 2));
            var expression = exp0.ExportAsString();

            var exp = expression.AsExpression<MyParams, double>();

            var paramz = new MyParams { Z = 14 };

            var result = exp.Compile().Invoke(paramz);
            var expected = exp0.Compile().Invoke(paramz);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AsExpression_Minus_CorrectResultReturned()
        {
            var expression = "_ => 5.1 - -6.2";

            var exp = expression.AsExpression<MyParams, double>();

            var result = exp.Compile().Invoke(new MyParams());

            Assert.That(result, Is.EqualTo(5.1 - -6.2));
        }

        [Test]
        public void AsExpression_Closure_CorrectResultReturned()
        {
            var i = new { x = 1 };
            var z = 123;

            var exp = Exp(i, 0, x => x.x + z);
            
            var exps = exp.ExportAsString();

            var func = exps.AsFunc(i, 0);

            var result = func();

            var expected = exp.Compile().Invoke(i);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AsExpression_AnonymousParameterExpression_CorrectResultReturned()
        {
            var i = new { x = 1, y = "s" };

            var exp = Exp(i, 0, x => x.x + x.y.Length);

            var exps = exp.ExportAsString();

            var func = exps.AsFunc(i, 0);

            var result = func();

            var expected = exp.Compile().Invoke(i);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AsExpression_Fractions_CorrectResultReturned()
        {
            var exp = FractionExp(x => x.Value1 + x.Value2);

            var func = exp.ExportAsString().AsExpression<MyParamsWithFractions, Fraction>().Compile();

            var input = new MyParamsWithFractions
            {
                Value1 = Fraction.ApproxPii,
                Value2 = Fraction.E
            };

            var result = func(input);

            var expected = exp.Compile().Invoke(input);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AsExpression_VectorOperation_CorrectResultReturned()
        {
            var exp = VectExp(x => x.Input1.ToColumnVector() * x.Input2.ToColumnVector());

            var exp2 = exp.ExportAsString().AsExpression<MyParamsWithVector, IVector>();

            var func = exp2.Compile();

            var input = new MyParamsWithVector
            {
                Input1 = new ColumnVector1D(1.1, 2.2),
                Input2 = new ColumnVector1D(3.1, 4.2)
            };

            var result = func(input);

            var expected = exp.Compile().Invoke(input);

            // [3.41,9.24]

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AsExpression_Vector_CorrectResultReturned()
        {
            var exp = VectExp(x => x.Input1.MultiplyBy(x.Input2));

            var func = exp.ExportAsString().AsExpression<MyParamsWithVector, IVector>().Compile();

            var input = new MyParamsWithVector
            {
                Input1 = new ColumnVector1D(1.1, 2.2),
                Input2 = new ColumnVector1D(3.1, 4.2)
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
        public void AsExpression_NumerousOperators_CreatesCorrectPrecedence(string expression, double expectedResult)
        {
            var func = expression.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { Z = 3, X = 12, Y = -4 });

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void AsExpression_FieldAccessor_CorrectResultReturned()
        {
            var exp = Exp(x => x.Field1 * (long)2);

            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { Field1 = 9 });

            Assert.That(result, Is.EqualTo(18));
        }

        [Test]
        public void AsExpression_ArrayAndConversion_CorrectResultReturned()
        {
            var exps = "x => [Convert((x), double), 2]";

            var func = exps.AsExpression<int, double[]>().Compile();

            var result = func(5);

            Assert.That(result[0], Is.EqualTo(5));
            Assert.That(result[1], Is.EqualTo(2));
        }

        [Test]
        public void AsExpression_VectorFunction_CorrectResultReturned()
        {
            var exp = Exp(1, new Vector(0), x => ColumnVector1D.Create(x, 2));

            var exps = exp.ExportAsString();

            var func = exps.AsExpression<int, Vector>().Compile();

            var result = func(5);

            Assert.That(result.Equals(ColumnVector1D.Create(5, 2)));
        }

        [Test]
        public void AsExpression_MathFunction_CorrectResultReturned()
        {
            var exp = Exp(x => Math.Sqrt(x.Z)); // "x => Sqrt(x.Z)"

            var expStr = exp.ExportAsString();
            var func = expStr.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { Z = 9 });

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void AsExpression_MultiplyExpressionWithLeftConversion_CorrectResultReturned()
        {
            const string exps = "x => x.X * 1443.22";

            var func = exps.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { X = 2 });

            Assert.That(result, Is.EqualTo(2d * 1443.22));
        }

        [Test]
        public void AsExpression_MultiplyExpressionWithRightConversion_CorrectResultReturned()
        {
            const string exps = "x => 1443.22 + x.X ";

            var func = exps.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { X = 2 });

            Assert.That(result, Is.EqualTo(2d + 1443.22));
        }

        [Test]
        public void AsExpression_MultiplyExpressionWithConversion_CorrectResultReturned()
        {
            var exp = Exp(x => x.X * x.Y); // "x => Convert((x.X * x.Y), Double)"
            var exps = exp.ExportAsString();

            var func = exps.AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { X = 2, Y = 3 });

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void AsExpression_AdditionExpression_CorrectResultReturned()
        {
            var exp = Exp(x => x.Z + 2.2d);

            var func = exp.ExportAsString().AsExpression<MyParams, double>().Compile();

            var result = func(new MyParams { Z = 1.1 });

            Assert.That(Math.Round(result, 5), Is.EqualTo(3.3));
        }

        static Expression<Func<TInput, TOuput>> Exp<TInput, TOuput>(TInput inputExample, TOuput outputExample,
            Expression<Func<TInput, TOuput>> exp) => exp;

        static Expression<Func<MyParams, double>> Exp(Expression<Func<MyParams, double>> exp) => exp;

        static Expression<Func<MyParamsWithVector, IVector>> VectExp(Expression<Func<MyParamsWithVector, IVector>> exp) => exp;

        static Expression<Func<MyParamsWithFractions, Fraction>> FractionExp(Expression<Func<MyParamsWithFractions, Fraction>> exp) => exp;

        class MyParams
        {
            public long Field1 { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public double Z { get; set; }
            public bool P { get; set; }
            public bool PFValue { get; set; } = true;
            public bool PF() => PFValue;
        }

        class ParamsWithIndexedProperty
        {
            public int this[int n] => n * 7;
        }

        class MyParamsWithVector
        {
            public IVector Input1 { get; set; }
            public IVector Input2 { get; set; }
        }

        class MyParamsWithFractions
        {
            public Fraction Value1 { get; set; }
            public Fraction Value2 { get; set; }
        }
    }
}
