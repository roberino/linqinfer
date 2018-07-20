using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class ActivatorExpressionTests
    {
        [TestCase(nameof(Activators.Sigmoid))]
        [TestCase(nameof(Activators.HyperbolicTangent))]
        [TestCase(nameof(Activators.Threshold))]
        public void Export_WhenParsed_CreatedSameResults(string name)
        {
            var activator = Activators.FindByName(name);

            var data = activator.Export();

            var activator2 = ActivatorExpression.Parse(data);

            var result1 = activator.Activator(0.123);
            var result2 = activator2.Activator(0.123);

            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(activator.Name, Is.EqualTo(activator2.Name));
        }
    }
}
