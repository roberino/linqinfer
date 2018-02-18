using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    public class FunctionIteratorTests
    {
        [Test]
        public void WhenGivenFunctionWithoutHaltCondition_ThenIterationCompletes()
        {
            var iterator = new FunctionIterator<int>(x => x + 1);

            var result = iterator.IterateFunction(3, 20, 2);

            Assert.That(result.ActualIterations, Is.EqualTo(20));
            Assert.That(result.WasHalted, Is.False);
            Assert.That(result.Outputs.Length, Is.EqualTo(2));
            Assert.That(result.Outputs[0], Is.EqualTo(23));
            Assert.That(result.Outputs[1], Is.EqualTo(22));
        }


        [Test]
        public void WhenGivenFunctionWithHaltConditionAndHaltValueNotIncluded_ThenIterationCompletes()
        {
            var iterator = new FunctionIterator<int>(x => x + 1);

            var result = iterator.IterateFunction(3, 20, 3, (n, x) => { }, x => x > 14);

            Assert.That(result.ActualIterations, Is.EqualTo(12));
            Assert.That(result.WasHalted, Is.True);
            Assert.That(result.Outputs.Length, Is.EqualTo(3));
            Assert.That(result.Outputs[0], Is.EqualTo(14));
            Assert.That(result.Outputs[1], Is.EqualTo(13));
            Assert.That(result.Outputs[2], Is.EqualTo(12));
        }

        [Test]
        public void WhenGivenFunctionWithHaltConditionAndHaltValueIncluded_ThenIterationCompletes()
        {
            var iterator = new FunctionIterator<int>(x => x + 1);

            var result = iterator.IterateFunction(3, 20, 3, (n, x) => { }, x => x > 14, true);

            Assert.That(result.ActualIterations, Is.EqualTo(12));
            Assert.That(result.WasHalted, Is.True);
            Assert.That(result.Outputs.Length, Is.EqualTo(3));
            Assert.That(result.Outputs[0], Is.EqualTo(15));
            Assert.That(result.Outputs[1], Is.EqualTo(14));
            Assert.That(result.Outputs[2], Is.EqualTo(13));
        }
    }
}