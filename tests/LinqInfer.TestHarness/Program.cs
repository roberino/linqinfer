using LinqInfer.Learning.Classification.Remoting;
using LinqInfer.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverEndpoint = new Uri("tcp://localhost:9033/");
            var data = new List<string>();

            while (true)
            {
                Console.Write(":");
                var text = Console.ReadLine();

                if (string.IsNullOrEmpty(text)) break;

                data.Add(text);
            }

            var pipeline = data.Select(t => new
            {
                text = t.Split('=').First(),
                cls = t.Split('=').Last()
            })
            .AsQueryable()
            .CreateTextFeaturePipeline(x => x.cls);

            var task = pipeline
                .ToMultilayerNetworkClassifier(x => x.cls, serverEndpoint, false, 0.1f, pipeline.VectorSize * 2, pipeline.VectorSize * 2);

            task.Wait();

            var nn = task.Result;

            Console.WriteLine();
            Console.WriteLine();

            while (true)
            {
                var next = Console.ReadLine();

                if (string.IsNullOrEmpty(next)) break;

                var results = nn.Classify(new
                {
                    text = next,
                    cls = "?"
                });

                foreach (var res in results)
                {
                    Console.WriteLine(res);
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
