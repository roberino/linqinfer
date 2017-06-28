using LinqInfer.Genetics;
using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
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
        public async Task ShakespeareToGraph1()
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var shakespeare = new Corpus(corpusStream.Tokenise());

                var graph = await shakespeare.ExportWordGraph("hate");

                var gexf = await graph.ExportAsGexfAsync();

                // gexf.Save(@"C:\git\shakespeare2.gexf");
            }
        }

        [Test]
        public async Task ShakespeareToGraph()
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var shakespeare = new Corpus(corpusStream.Tokenise()).Blocks.Take(50).ToList();

                var graph = new WeightedGraph<string, int>(new WeightedGraphInMemoryStore<string, int>(), (x, y) => x + y);

                WeightedGraphNode<string, int> last;

                foreach (var block in shakespeare)
                {
                    last = null;

                    foreach (var token in block.Select(t => t.Text.ToLower()))
                    {
                        if (last == null)
                            last = await graph.FindOrCreateVertexAsync(token);
                        else
                            last = await last.ConnectToOrModifyWeightAsync(token, 1, w => w + 1);
                    }
                }

                await graph.SaveAsync();

                var path = await graph.OptimalPathSearch.FindBestPathAsync("the", "love", (w1, w2) => w1 < w2 ? 1 : (w1 > w2 ? -1 : 0));

                foreach(var p in path)
                {
                    Console.WriteLine("=> {0} [{1}]", p.Key, p.Value);
                }
            }
        }

        [Test]
        public void ClassificationOfShakespearTextVsRandomGeneratedText()
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var shakespeare = new Corpus(corpusStream.Tokenise()).Blocks.Take(300).ToList();
                var monkeyText = new Corpus(GenerateMonkeyText(300).Tokenise()).Blocks.Take(15).ToList();

                var data = shakespeare.Take(15).Select(s => new
                {
                    text = s.Aggregate(new StringBuilder(), (b, t) => b.Append(t).Append(' ')).ToString(),
                    cls = "s"
                })
                .Concat(monkeyText.Take(15).Select(s => new
                {
                    text = s.Aggregate(new StringBuilder(), (b, t) => b.Append(t).Append(' ')).ToString(),
                    cls = "m"
                }))
                .RandomOrder()
                .ToList()
                .AsQueryable();

                var ao = new AlgorithmOptimiser();

                var vectorSize = ao.Parameters.DefineInteger("vectorSize", 30, 2048, 30);
                var pcaFactor = ao.Parameters.DefineInteger("pcaFactor", 0, 60, 10);
                var errorTolerance = ao.Parameters.DefineDouble("errorTolerance", 0.003, 0.05, 0.01);
                var learningRate = ao.Parameters.DefineDouble("learningRate", 0.01, 0.3, 0.1);
                var hiddenLayer = ao.Parameters.DefineInteger("hiddenLayer", 0, 80, 4);

                var optimalParams = ao.Optimise(p =>
                {
                    var pipeline = data.CreateTextFeaturePipeline(a => a.cls, vectorSize);

                    if (pcaFactor > 1) pipeline.PrincipalComponentReduction(pcaFactor);

                    var trainingSet = pipeline.AsTrainingSet(c => c.cls);
                    var classifier = trainingSet.ToMultilayerNetworkClassifier(learningRate, hiddenLayer).Execute();

                    int t = 0;

                    foreach (var n in Enumerable.Range(1, 10))
                    {
                        var expectedClass = n > 5 ? "s" : "m";

                        var testCase = new
                        {
                            text = expectedClass == "m" ? GenerateMonkeyText(10) : shakespeare.Skip(15).RandomOrder().FirstOrDefault().Aggregate(new StringBuilder(), (s, x) => s.Append(x).Append(" ")).ToString(),
                            cls = "?"
                        };

                        var result = classifier.Classify(testCase);

                        if (result.First().ClassType == expectedClass)
                        {
                            t++;
                        }

                        //foreach (var r in result) Console.WriteLine(r);
                    }

                    Console.WriteLine("Result {0}/10", t);

                    return t;
                }, 25);

                foreach (var p in optimalParams)
                {
                    Console.WriteLine("Optimal {0} = {1}", p.Key, p.Value);
                }
            }
        }

        private string GenerateMonkeyText(int numOfLines)
        {
            var text = new StringBuilder();

            foreach (var x in Enumerable.Range(0, numOfLines))
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
                foreach (var y in Enumerable.Range(0, Functions.Random(8) + 3))
                {
                    //text.Append(_dict.RandomWord() + " ");
                    text.Append(" " + rnd());
                }

                text.Append('.');
            }

            return text.ToString();
        }
    }
}