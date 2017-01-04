using LinqInfer.Genetics;
using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class ShakespeareVsMonkey : TestFixtureBase
    {
        private readonly EnglishDictionary _dict = new EnglishDictionary();

        [Test]
        public void ClassificationOfShakespearTextVsRandomGeneratedText()
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var shakespeare = new Corpus(corpusStream.Tokenise());
                var monkeyText = new Corpus(GenerateMonkeyText().Tokenise());

                var data = shakespeare.Blocks.Take(15).Select(s => new
                {
                    text = s.Aggregate(new StringBuilder(), (b, t) => b.Append(t).Append(' ')).ToString(),
                    cls = "s"
                })
                .Concat(monkeyText.Blocks.Take(15).Select(s => new
                {
                    text = s.Aggregate(new StringBuilder(), (b, t) => b.Append(t).Append(' ')).ToString(),
                    cls = "m"
                }))
                .RandomOrder()
                .ToList()
                .AsQueryable();

                var ao = new AlgorithmOptimiser();

                var vectorSize = ao.Parameters.DefineInteger("vectorSize", 30, 50, 30);
                var pcaFactor = ao.Parameters.DefineInteger("pcaFactor", 2, 60, 10);
                var errorTolerance = ao.Parameters.DefineDouble("errorTolerance", 0.003, 0.05, 0.01);

                ao.Optimise(p =>
                {
                    var pipeline = data.
                       CreateTextFeaturePipeline(x => Guid.NewGuid().ToString(), vectorSize);

                    foreach (var f in pipeline.FeatureExtractor.FeatureMetadata)
                    {
                        Console.WriteLine(f.Label);
                    }

                    pipeline.PrincipalComponentReduction(pcaFactor);

                    var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.cls, errorTolerance).Execute();

                    int t = 0;

                    foreach (var n in Enumerable.Range(1, 10))
                    {
                        var testCase = new
                        {
                            text = GenerateMonkeyText(),
                            cls = "?"
                        };

                        var result = classifier.Classify(testCase);

                        if (result.First().ClassType == "m")
                        {
                            t++;
                        }
                    }

                    return t;
                }, 5);

                //var pipeline = data.
                //    CreateTextFeaturePipeline(x => Guid.NewGuid().ToString(), 100);

                //foreach(var f in pipeline.FeatureExtractor.FeatureMetadata)
                //{
                //    Console.WriteLine(f.Label);
                //}

                //pipeline.PrincipalComponentReduction(64);

                //pipeline.ToCsv(Console.Out).Execute();
                
                //var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.cls, 0.07f).Execute();
                
                //foreach (var n in Enumerable.Range(1, 10))
                //{
                //    var testCase = new
                //    {
                //        text = GenerateMonkeyText(),
                //        cls = "?"
                //    };

                //    var result = classifier.Classify(testCase);

                //    Console.WriteLine(testCase.text);
                //    foreach (var r in result) Console.WriteLine(r);
                //}
            }
        }

        private string GenerateMonkeyText()
        {
            var text = new StringBuilder();

            foreach (var x in Enumerable.Range(0, 10))
            {
                text.Append(GenerateMonkeyParagraph());
                text.AppendLine();
                text.AppendLine();
            }

            return text.ToString();
        }

        private string GenerateMonkeyParagraph()
        {
            var text = new StringBuilder();
            var rnd = Functions.RandomPicker("monkey", "jungle", "banana", "vine", "tarzan");

            foreach (var x in Enumerable.Range(0, Functions.Random(8) + 1))
            {
                foreach (var y in Enumerable.Range(0, Functions.Random(8) + 1))
                {
                    //text.Append(_dict.RandomWord() + " ");
                    text.Append(rnd() + " ");
                }

                text.Append('.');
            }

            return text.ToString();
        }
    }
}