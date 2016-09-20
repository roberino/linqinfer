﻿using LinqInfer.Learning.Classification.Remoting;
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

            var pipeline = data.Select(t => new
            {
                text = t.Split('=').First(),
                cls = t.Split('=').Last()
            })
            .AsQueryable()
            .CreateTextFeaturePipeline(x => x.cls);

            var client = serverEndpoint.CreateMultilayerNetworkClient();

            var task = client
                .CreateClassifier(pipeline, x => x.cls, true, name, 0.1f, pipeline.VectorSize * 2, pipeline.VectorSize * 2);

            task.Wait();

            var nn = task.Result;

            Console.WriteLine();
            Console.WriteLine();

            while (true)
            {
                var next = Console.ReadLine();

                if (string.IsNullOrEmpty(next)) break;

                var results = nn.Value.Classify(new
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
