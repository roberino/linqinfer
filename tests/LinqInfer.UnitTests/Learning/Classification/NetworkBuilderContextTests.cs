using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class NetworkBuilderContextTests
    {
        [Test]
        public void WhenGivenActivator_ThenCanFormatAndRecreate()
        {
            var context = new NetworkBuilderContext();

            var sigmoid = Activators.Sigmoid(1.8);

            var sigmoidFormatted = context.ActivatorFormatter.Create(sigmoid);

            var sigmoid2 = context.ActivatorFactory.Create(sigmoidFormatted);

            Assert.That(sigmoid2.Name, Is.EqualTo(sigmoid.Name));
        }

        [Test]
        public void WhenGivenNeuronParams_ThenNeuronCreated()
        {
            var context = new NetworkBuilderContext();

            var neuronParams = new NeuronParameters(24, Activators.Sigmoid(), new LinqInfer.Maths.Range(15, -12));

            var neuron = context.NeuronFactory.Create(neuronParams);

            Assert.That(neuron.Size, Is.EqualTo(24));
            Assert.That(neuron.Activator, Is.Not.Null);
        }
    }
}