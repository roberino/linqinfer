using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using static LinqInfer.Tests.TestData;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class PipelineExtensionsTests
    {
        [Test]
        public void ToSofm_WithoutSupplingRadius()
        {
            var data = Enumerable.Range(1, 10).Select(n => Functions.RandomVector(2)).ToList().AsQueryable();
            var pipeline = data.CreatePipeline();
            var map = pipeline.ToSofm(3).Execute();

            foreach (var m in map)
            {
                Console.WriteLine("{0} - {1}", m.Key, m.Count());
            }
        }

        [Test]
        public void ToSofm_WithAnInitialRadius()
        {
            var data = Enumerable.Range(1, 10).Select(n => Functions.RandomVector(2)).ToList().AsQueryable();
            var pipeline = data.CreatePipeline();
            var map = pipeline.ToSofm(3, 0.2f, 0.1f).Execute();

            foreach (var m in map)
            {
                Console.WriteLine("{0} - {1}", m.Key, m.Count());
            }
        }

        [Test]
        public void ToNaiveBayesClassifier_SimpleSample_ClassifiesAsExpected()
        {
            var pirateSample = TestData.CreatePirates().ToList();
            var pipeline = pirateSample.AsQueryable().CreatePipeline();
            var classifier = pipeline.ToNaiveBayesClassifier(p => p.Age > 25 ? "old" : "young").Execute();
            
            // In the original predicate, if age > 25 then old.
            // But this pirate shares many features of other young pirates
            // So therfore should be classed as "young"
            var classOfPirate = classifier.Classify(new TestData.Pirate()
            {
                Gold = 120,
                Age = 27,
                IsCaptain = false,
                Ships = 1
            }).FirstOrDefault();

            var classOfPirate2 = classifier.Classify(new TestData.Pirate()
            {
                Gold = 1600,
                Age = 41,
                IsCaptain = true,
                Ships = 4
            }).FirstOrDefault();
            
            Assert.That(classOfPirate.ClassType, Is.EqualTo("young"));
            Assert.That(classOfPirate2.ClassType, Is.EqualTo("old"));
        }


        [Test]
        public void ToMultilayerNetworkClassifier_XorSample_ClassifiesAsExpected()
        {
            int i = 0;

            var xor1 = new XorNode() { X = true, Y = false };
            var xor2 = new XorNode() { X = false, Y = false };
            var xor3 = new XorNode() { X = true, Y = true };
            var xor4 = new XorNode() { X = false, Y = true };

            var samples = new[] { xor1, xor2, xor3, xor4 };
            var pipeline = samples.AsQueryable().CreatePipeline();
            
            var classifier = pipeline
                .ToMultilayerNetworkClassifier(x => x.Output, 0.3f)
                .ExecuteUntil(f =>
            {
                i++;

                if (i > 200) return true;

                return
                    f.Classify(xor1).First().ClassType == xor1.Output
                    && f.Classify(xor2).First().ClassType == xor2.Output
                    && f.Classify(xor3).First().ClassType == xor3.Output
                    && f.Classify(xor4).First().ClassType == xor4.Output;
            });

            var classResults1 = classifier.Classify(xor1).First();
            var classResults2 = classifier.Classify(xor2).First();
            var classResults3 = classifier.Classify(xor3).First();
            var classResults4 = classifier.Classify(xor4).First();

            Assert.That(classResults1.ClassType == xor1.Output);
            Assert.That(classResults2.ClassType == xor2.Output);
            Assert.That(classResults3.ClassType == xor3.Output);
            Assert.That(classResults4.ClassType == xor4.Output);
        }

        [Test]
        public void ToMultilayerNetworkClassifier_SaveOutput_RestoresAsExpected()
        {
            var pirateSample = CreatePirates().ToList();
            var pipeline = pirateSample.AsQueryable().CreatePipeline();

            using (var blob = new InMemoryBlobStore())
            {
                pipeline.OutputResultsTo(blob);

                var classifier = pipeline
                    .ToMultilayerNetworkClassifier(p => p.Age > 25 ? "old" : "young", 0.1f)
                    .Execute("x");

                var classifier2 = blob.OpenMultilayerNetworkClassifier<Pirate, string>("x");

                Assert.That(classifier2, Is.Not.Null);

                var cls = classifier2.Classify(new Pirate() { Age = 10, Gold = 2, IsCaptain = false, Ships = 1 });

                Assert.That(cls.Any(x => x.Score > 0));
            }
        }

        [Test]
        public void ToMultilayerNetworkClassifier_UsingParametersSimpleSample_ClassifiesAsExpected()
        {
            var pirateSample = CreatePirates().ToList();
            var pipeline = pirateSample.AsQueryable().CreatePipeline();
            var classifier = pipeline
                .ToMultilayerNetworkClassifier(p => p.Age > 25 ? "old" : "young", 8)
                .Execute();

            var results = classifier.Classify(new Pirate()
            {
                Gold = 120,
                Age = 5,
                IsCaptain = false,
                Ships = 1
            });

            foreach (var cls in results)
            {
                Console.WriteLine(cls);
            }
        }

        [Test]
        public async Task ToMultilayerNetworkClassifier_AsyncExample()
        {
            var pirateSample = CreatePirates().ToList();
            var pipeline = pirateSample.AsQueryable().CreatePipeline();
            var trainingSet = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? "x" : "y");
            var classifier = await trainingSet.ToMultilayerNetworkClassifier().ExecuteAsync();

            var classOfPirate = classifier.Classify(new Pirate()
            {
                Gold = 120,
                Age = 5,
                IsCaptain = false,
                Ships = 1
            }).FirstOrDefault();

            var score = classifier.ClassificationAccuracyPercentage(trainingSet);

            Console.WriteLine(score);

            Assert.That(classOfPirate, Is.Not.Null);
        }

        [Test]
        public void ToMultilayerNetworkClassifier_SimpleSample_ClassifiesAsExpected()
        {
            int successCounter = 0; int failureCounter = 0;

            foreach (var i in Enumerable.Range(1, 25))
            {
                var pirateSample = CreatePirates().ToList();
                var pipeline = pirateSample.AsQueryable().CreatePipeline();
                var classifier = pipeline.ToMultilayerNetworkClassifier(p => p.Age > 25 ? "old" : "young", errorTolerance: 0.1f).Execute();
                
                var classOfPirate = classifier.Classify(new Pirate()
                {
                    Gold = 120,
                    Age = 5,
                    IsCaptain = false,
                    Ships = 1
                }).FirstOrDefault();

                var classOfPirate2 = classifier.Classify(new Pirate()
                {
                    Gold = 1600,
                    Age = 61,
                    IsCaptain = true,
                    Ships = 7
                }).FirstOrDefault();

                try
                {
                    Assert.That(classOfPirate.ClassType, Is.EqualTo("young"));
                    Assert.That(classOfPirate2.ClassType, Is.EqualTo("old"));
                    Console.WriteLine("=> SUCCESS");
                    successCounter++;
                }
                catch
                {
                    Console.WriteLine("=> FAILURE");
                    failureCounter++;
                }
            }

            Console.WriteLine("successes:{0},failures:{1}", successCounter, failureCounter);
            Assert.That((float)successCounter / (float)failureCounter, Is.GreaterThan(2.5f));
        }
    }
}
