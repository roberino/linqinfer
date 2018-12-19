using LinqInfer.Utility.Expressions;
using NUnit.Framework;
using System;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class ControlFunctionsTests
    {
        [Test]
        public void AsExpression_Property_ReturnsFallbackForMissingProperty()
        {
            var exp = "x => Property(x, Max, 1.1)"
                .AsExpression<TestData.Pirate, double>();

            var f = exp.Compile();

            var result = f(new TestData.Pirate());

            Assert.That(result, Is.EqualTo(1.1));
        }

        [Test]
        public void AsExpression_Property_ReturnsValueForExistingProperty()
        {
            var exp = "x => Property(x, Age, 1.1)"
                .AsExpression<TestData.Pirate, double>();

            var f = exp.Compile();

            var result = f(new TestData.Pirate(){ Age = 12 });

            Assert.That(result, Is.EqualTo(12));
        }

        [Test]
        public void AsExpression_HasProperty_EvaluatesAsTrueForPublicProperty()
        {
            var exp = "x => HasProperty(x, Age) ? 1.1 : 2.2"
                .AsExpression<TestData.Pirate, double>();

            var f = exp.Compile();

            var result = f(new TestData.Pirate());

            Assert.That(result, Is.EqualTo(1.1));
        }

        [Test]
        public void AsExpression_HasProperty_EvaluatesAsFalseForUndefinedProperty()
        {
            var exp = "x => HasProperty(x, Max) ? 1.1 : 2.2"
                .AsExpression<TestData.Pirate, double>();

            var f = exp.Compile();

            var result = f(new TestData.Pirate());

            Assert.That(result, Is.EqualTo(2.2));
        }

        [Test]
        public void AsExpression_LoopThenCondition_EvaluatesCorrectExpression()
        {
            var exp = ("x => Loop(y => y + x, x)" +
                      ".Then(z => z.Length > 5 ? 5.0 : 6.0)")
                .AsExpression<int, double>();

            var f = exp.Compile();

            var result = f(2);

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void AsExpression_PLoop_ReturnsCorrectSizedResult()
        {
            var exp = "x => PLoop(y => Loop(z => z * 5, x).Model.Sum(), 5).Model.Length"
                .AsExpression<int, int>();

            var f = exp.Compile();

            var result = f(20);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void AsExpression_WithIf_EvaluatesCorrectExpression()
        {
            var exp = "(x:double) => If(x, x1 => x1 > 5, x1 => x1 + 5, x1 => x1 + 6)"
                .AsExpression<int, double>();

            var f = exp.Compile();

            var result = f(2);

            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void AsExpression_WithPromise_IsConvertedToRequiredType()
        {
            var exp = "x => Loop(i => 5 * i, Do(() => 1 + x)).Model[1]".AsExpression<int, double>();

            var f = exp.Compile();

            var result = f(2);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void Result_IsSameEachInvocation()
        {
            var promise = new Promise<string>(() => Guid.NewGuid().ToString());

            Assert.That(promise.Result, Is.EqualTo(promise.Result));
        }
        
        [Test]
        public void Then_ReturnsExpectedResult()
        {
            var result = "abcd";

            var promise = new Promise<string>(() => result).Then(s => s[1]);

            Assert.That(promise.Result, Is.EqualTo('b'));
        }
        
        [Test]
        public void Then_ReturnsNewResultFromPreviousResult()
        {
            var promise = new Promise<string>(() => Guid.NewGuid().ToString())
                .Then(s => s[0]);

            Assert.That(promise.Result, Is.EqualTo(promise.Result));
        }
    }
}
