﻿using LinqInfer.Utility.Expressions;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class CompileTimeExpressionParserTests
    {
        [Test]
        public void AsExpression_WithReferenceFunc_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "a => FuncB(a + 1)";

            var lambda = funcA.AsExpression<int, int>(sourceCode);

            var program = lambda.Compile();

            var result = program.Invoke(123);

            Assert.That(result, Is.EqualTo(124 * 5d));
        }

        [Test]
        public void AsFunc_SingleParameter_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "a => FuncB(a + 1)";

            var func = funcA.AsFunc<int>(sourceCode, p => p.Name == "a" ? 123 : 0);

            var result = func();

            Assert.That(result.Result, Is.EqualTo(124 * 5d));
        }

        [Test]
        public void AsFunc_SingleTypedParameter_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "(a: double) => FuncB(a + 1)";

            var func = funcA.AsFunc<double>(sourceCode, p => Convert.ChangeType("123", p.Type));

            var result = func();

            Assert.That(result.Result, Is.EqualTo(124 * 5d));
        }

        [Test]
        public void AsFunc_InnerLamda_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "x => FuncC(() => 15 + x)";

            var func = funcA.AsFunc<int>(sourceCode, p => 5);

            var result = func();

            Assert.That(result.Result, Is.EqualTo(100d));
        }
        
        [Test]
        public void AsFunc_SingleTypedFuncParameter_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "(myFx: func(double, double)) => myFx(123)";
            
            var func = funcA.AsFunc<double>(sourceCode, p => Exp<double, double>(x => x * 15).Compile());

            var result = func();

            Assert.That(result.Result, Is.EqualTo(123 * 15d));
        }

        [Test]
        public void AsFunc_SingleTypedExpParameter_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "(myFx: func(double, double)) => myFx(123)";
            
            var func = funcA.AsFunc<double>(sourceCode, p => Exp<double, double>(x => x * 15));

            var result = func();

            Assert.That(result.Result, Is.EqualTo(123 * 15d));
        }

        [Test]
        public void AsFunc_TwoParameters_ReturnsExpectedResult()
        {
            var sourceCode = SetupProvider();

            var funcA = "(a, b) => FuncB(a + 1 + b)";

            var func = funcA.AsFunc<int>(sourceCode, p =>
            {
                switch (p.Name)
                {
                    case "a": return 123;
                    case "b": return 456;
                    default: return 0;
                }
            });

            var result = func();

            Assert.That(result.Result, Is.EqualTo((123 + 1 + 456) * 5d));
        }

        
        static Expression<Func<TIn, TOut>> Exp<TIn, TOut>(Expression<Func<TIn, TOut>> exp) => exp;

        static DelegatingSourceCodeProvider SetupProvider()
        {
            return new DelegatingSourceCodeProvider(n =>
            {
                if (n == "FuncB")
                {
                    return "x => x * 5";
                }

                if (n == "FuncC")
                {
                    return "(x:func(int)) => x() * 5";
                }

                return null;
            });
        }
    }
}