using LinqInfer.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class ProbabilityExtensionsTests
    {

        [Test]
        public void BayesianProbability_MedicalTestExample()
        {
            // example from http://vassarstats.net/bayes.html

            // Manual calculations

            var disease = (1).OutOf(200);  // A = P(of disease)
            var notDisease = disease.Compliment(); // ~A = P(of healthy)

            var truePositive = (99).OutOf(100); // Positive test when disease present
            var falseNegative = truePositive.Compliment(); // Negative test when disease present
            var falsePositive = (5).OutOf(100); // Positive test when healthy
            var trueNegative = falsePositive.Compliment(); // Negative test when healthy

            var B = truePositive * disease + falsePositive * notDisease; // B = P(of positive result)

            var BgivenA = disease * truePositive / B;

            Assert.That(BgivenA.Value, Is.InRange(0.09045, 0.0905));

            // Problem represented as fractions converted hypotheses

            var hypos = P.Hypotheses((1).OutOf(200), (199).OutOf(200));

            hypos.Update(truePositive, falsePositive);

            Assert.That(hypos.Hypotheses.First().PosteriorProbability, Is.EqualTo(BgivenA));

            // Problem represented as outcome classes

            var diseaseHypo = P.Of("A").Is(1).OutOf(200);
            var healthyHypo = P.Of("~A").Is(diseaseHypo.PriorProbability.Compliment());

            P.Hypotheses(diseaseHypo, healthyHypo).Update(truePositive, falsePositive);

            Assert.That(diseaseHypo.PosteriorProbability, Is.EqualTo(BgivenA));
        }

        [Test]
        public void AsSampleSpace_AreMutuallyExclusive_ReturnsTrue_ForUniqueItems()
        {
            var sample = TestData.CreateQueryablePirates().AsSampleSpace();

            Assert.That(sample.AreMutuallyExclusive(p => p.Age == 25, p => p.Gold == 1600));
        }

        [Test]
        public void AsSampleSpace_Count_ReturnsCorrectValue()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.Total(), Is.EqualTo(testData.ToList().Count));
        }

        [Test]
        public void AsSampleSpace_ProbabilityOfEvent_ReturnsCorrectValue()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();
            int count = testData.ToList().Count;
            var pExpected = new Fraction(2, count);
            var pActual = sample.ProbabilityOfEvent(p => p.Age == 25 || p.Gold == 1600);

            Assert.That(pActual, Is.EqualTo(pExpected));
            Assert.That(pActual.Value, Is.EqualTo(2d / count));
        }

        [Test]
        public void AsSampleSpace_IsExhaustive_ReturnsTrueForPredicateSelectingAll()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsExhaustive(p => p.Age > 0), Is.True);
        }

        [Test]
        public void AsSampleSpace_IsSimple_ReturnsTrueForPredicateSelectingOne()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsSimple(p => p.Gold >= 1800), Is.True);
        }

        [Test]
        public void AsSampleSpace_CreateHypothesesis()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            var hypo1 = sample.CreateHypothesis(p => p.Age > 25);
            var hypo2 = sample.CreateHypothesis(p => p.Age <= 25);

            hypo1.Update(p => p.Gold > 100);
            hypo2.Update(p => p.Gold > 100);

            Console.WriteLine("Hypothesis 1: Gold > 500 = {0}", hypo1.PosteriorProbability);
            Console.WriteLine("Hypothesis 2: Gold > 500 = {0}", hypo2.PosteriorProbability);
        }
    }
}
