using LinqInfer.Language;
using LinqInfer.Learning;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests
{
    [TestFixture]
    public class Samples
    {   
        [Test]
        public void CombineWordSearchAsHypothesis()
        {
            var dict = new EnglishDictionary();

            var possibleMatches = dict.FindWordsLike("businesses").AsHypotheses();

            possibleMatches.Update(w => w.StartsWith("business") && !w.EndsWith("es") ? (1).OutOf(1) : (1).OutOf(2));

            var mostProbable = possibleMatches.MostProbable();

            Assert.That(mostProbable, Is.EqualTo("businessmen"));
        }

        [Test]
        public void CombineClassifier_WithHypotheses()
        {
            var pirateSample = TestData.CreatePirates().ToList();
            var classifier = pirateSample.AsQueryable().ToSimpleDistributionFunction(p => p.Age > 25 ? "old" : "young");

            var distribution = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 120,
                Age = 26,
                IsCaptain = false,
                Ships = 1
            });

            var hypos = distribution.Select(x => P.Hypothesis(x.Key, x.Value)).AsHypotheses();

            hypos.Update(x => x == "old" ? (5).OutOf(6) : (1).OutOf(10));

            var newPosterier = hypos["old"];
        }

        [Test]
        public void SelfOrganisingFeatureMap()
        {
            const int max = 100;

            var rnd = new Random(DateTime.Now.Millisecond);

            var cubes = Enumerable.Range(1, 1000).Select(n => new
            {
                height = rnd.Next(max),
                width = rnd.Next(max),
                depth = rnd.Next(max)
            }).AsQueryable();

            var map = cubes.ToSofm(new { height = max, width = max, depth = max }, 10);
            
            foreach (var m in map)
            {
                Console.WriteLine(string.Format("{0}\t{1}\t{2}", m.Weights[0], m.Weights[1], m.Weights[2]));
            }
        }
    }
}
