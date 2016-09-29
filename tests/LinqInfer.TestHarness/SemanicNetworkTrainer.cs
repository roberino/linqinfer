using LinqInfer.Learning;
using LinqInfer.Learning.Classification.Remoting;
using LinqInfer.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.TestHarness
{
    class SemanicNetworkTrainer
    {
        public void Run()
        {
            var serverEndpoint = new Uri("tcp://localhost:9034/");
            var data = new List<string>();

            Console.Write("Name:");
            var name = Console.ReadLine();

            while (true)
            {
                Console.Write(":");
                var text = Console.ReadLine();

                if (string.IsNullOrEmpty(text)) break;

                data.Add(text);
            }

            var trainingSet = data.Select(t => new
            {
                text = t.Split('=').First(),
                cls = t.Split('=').Last()
            })
            .AsQueryable()
            .CreateTextFeaturePipeline(x => x.cls)
            .AsTrainingSet(x => x.cls);

            var client = serverEndpoint.CreateMultilayerNeuralNetworkClient();

            var task = client
                .CreateClassifier(trainingSet, true, name, 0.1f, trainingSet.FeaturePipeline.VectorSize * 2, trainingSet.FeaturePipeline.VectorSize * 2);

            task.Wait();

            var nn = task.Result;

            Console.WriteLine();
            Console.WriteLine();

            var n = nn.Value;

            while (true)
            {
                var next = Console.ReadLine();

                if (string.IsNullOrEmpty(next)) break;

                var indent = 0;

                while (n != null)
                {
                    var results = n.Classify(new
                    {
                        text = next,
                        cls = "?"
                    });

                    foreach (var res in results)
                    {
                        Console.WriteLine(res.ToString().PadLeft(indent));
                    }

                    try
                    {
                        n = client.RestoreClassifier(new Uri(nn.Key, results.First().ClassType), trainingSet.FeaturePipeline.FeatureExtractor, "?").Result;
                    }
                    catch
                    {
                        n = null;
                    }

                    indent++;
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
