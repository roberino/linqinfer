using LinqInfer.Utility.Expressions;
using NUnit.Framework;
using System;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    [TestFixture]
    public class PromiseTests
    {
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
