using System;
using System.Linq;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class ProbabilityExtensionsTests
    {
        [Test]
        public void BayesianProbability_MixedDiceExample()
        {
            var die = new[] // A mixed bag of die with various number of faces
            {
                new
                {
                    n = 3,
                    faces = 4
                },
                new
                {
                    n = 2,
                    faces = 6
                },
                new
                {
                    n = 6,
                    faces = 12
                }
            };

            var hypos = die
                .Select(d => P.Of(d).Is(d.n).OutOf(die.Length))
                .AsHypotheses();

            var dist = hypos
                .DistributionOver((h, v) => v > h.faces ? Fraction.Zero : (1).OutOf(h.faces) * h.n, Enumerable.Range(1, 12));

            foreach (var h in dist)
            {
                Console.Write("{0}\t", h.Key.faces);

                foreach (var v in h.Value)
                {
                    Console.Write("{0}\t", v.Value);
                }
                Console.WriteLine();
            }
        }

        [Test]
        public void BayesianProbability_DiceExample()
        {
            // Given the dice below
            // What are the respective probabilities
            // that each dice was rolled if the roll = 6

            var die = new[] { 4, 6, 8, 12, 20 };
            var hypos = die.Select(n => P.Of(n).Is(1).OutOf(die.Length)).AsHypotheses();

            hypos.Update(x => x < 6 ? Fraction.Zero : (1).OutOf(x));

            Assert.That(hypos.ProbabilityOf(4), Is.EqualTo(Fraction.Zero));
            Assert.That(hypos.ProbabilityOf(8).Value, Is.InRange(0.294f, 0.295f));
        }

        [Test]
        public void BayesianProbability_MedicalTestExample()
        {
            // example from http://vassarstats.net/bayes.html

            // Manual calculations

            var disease = (1).OutOf(200);  // A = P(of disease)
            var notDisease = disease.Compliment(); // ~A = P(of healthy)

            Assert.That(disease + notDisease, Is.EqualTo(Fraction.One));

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
        public void AsSampleSpace_Various()
        {
            var sample = new[] { 1, 1, 2, 3, 5, 8, 13 }.AsQueryable().AsSampleSpace();

            Assert.That(sample.ProbabilityOfEvent(x => x == 1), Is.EqualTo((2).OutOf(7)));
            Assert.That(sample.ProbabilityOfEvent(x => x > 2), Is.EqualTo((4).OutOf(7)));
            Assert.That(sample.ProbabilityOfEventAorB(x => x > 2, y => y < 2), Is.EqualTo((6).OutOf(7)));
            Assert.That(sample.ProbabilityOfEventAandB(x => x > 2, y => y < 2), Is.EqualTo((0).OutOf(7)));
            Assert.That(sample.ProbabilityOfAny(x => x > 2, y => y < 2), Is.EqualTo((6).OutOf(7)));
            Assert.That(sample.ProbabilityOfAll(x => x > 2, y => y < 2), Is.EqualTo((0).OutOf(7)));
            Assert.That(sample.IsExhaustive(x => x > 0), Is.True);
            Assert.That(sample.IsSimple(x => x == 8), Is.True);
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
        public void PosterierProbabilityOfEventBGivenA()
        {
            var testData = new[] { 1, 2, 3, 4 }.AsQueryable();
            var sample = testData.AsSampleSpace();
            var cp = sample.PosterierProbabilityOfEventBGivenA(x => x == 1, x => x > 0 && x < 3);
            var exp = (1).OutOf(2);
            Assert.That(cp.Equals(exp));
        }

        [Test]
        public void AsSampleSpace_ConditionalProbabilityOfEventAGivenB()
        {
            var testData = new[] { 1, 2, 3, 4 }.AsQueryable();
            var sample = testData.AsSampleSpace();
            var cp = sample.ConditionalProbabilityOfEventAGivenB(x => x == 1, x => x < 3);
            var exp = (1).OutOf(2);
            Assert.That(cp.Equals(exp));
        }

        [Test]
        public void AsSampleSpace_ProbabilityOfEventAandB_ReturnsZeroForExclusiveEvents()
        {
            var testData = new[] { 1, 2, 3, 4 }.AsQueryable();
            var sample = testData.AsSampleSpace();
            var cp = sample.ProbabilityOfEventAandB(x => x > 1, x => x < 1);

            Assert.That(cp.Equals(0));
            Assert.That(sample.AreMutuallyExclusive(x => x > 1, x => x < 1));
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

        [Test]
        public void AsSampleSpace_IsExhaustive_ReturnsCorrectResult()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsExhaustive(p => p.Age > 0));
            Assert.That(sample.IsExhaustive(p => p.IsCaptain), Is.False);
        }

        [Test]
        public void AsSampleSpace_IsSimple_ReturnsCorrectResult()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsSimple(p => p.Gold == 101));
            Assert.That(sample.IsSimple(p => p.Gold > 101), Is.False);
        }

        [Test]
        public void AsSampleSpace_ProbabilityOfAll_ReturnsCorrectResult()
        {
            var testData = new int[] { 1, 5, 8 }.AsQueryable();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.ProbabilityOfAll(x => x > 1, x => x < 16, x => x != 4), Is.EqualTo((2).OutOf(3)));
        }

        [Test]
        public void AsSampleSpace_ProbabilityOfAny_ReturnsCorrectResult()
        {
            var testData = new int[] { 1, 5, 8 }.AsQueryable();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.ProbabilityOfAny(x => x > 1, x => x < 16, x => x != 4), Is.EqualTo((3).OutOf(3)));
        }
    }
}