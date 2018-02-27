﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LinqInfer.Benchmarking.DataSetup;

namespace LinqInfer.Benchmarking
{
    [HtmlExporter]
    [CsvExporter]
    [MarkdownExporter]
    public class MultilayerNetworkBenchmarks
    {
        IQueryable<DataItem> _dataSet;

        [GlobalSetup]
        public void Setup()
        {
            _dataSet = GetDataSet();
        }

        [Params(1, 2, 3)]
        public int NumberOfHiddenLayers { get; set; }

        [Params(2, 4)]
        public int LayerSize { get; set; }

        [Params("Sigmoid")]
        public string Activator { get; set; }

        [Benchmark]
        public void AttachMultilayerNetworkClassifier_Run()
        {
            CreateAsync().Wait();
        }

        private async Task CreateAsync()
        {
            var trainingSet = await _dataSet
                  .AsAsyncEnumerator()
                  .BuildPipelineAsync(CancellationToken.None)
                  .AsTrainingSetAsync(x => x.ClassName, CancellationToken.None);

            var network = trainingSet.AttachMultilayerNetworkClassifier(p =>
            {
                foreach (var x in Enumerable.Range(0, NumberOfHiddenLayers))
                {
                    p.AddHiddenLayer(new LayerSpecification(
                        LayerSize,
                        Activators.Create(Activator, 1),
                        LossFunctions.Default, new Range(1, -1)));
                }
            });

            await trainingSet.RunAsync(CancellationToken.None);

            var test = TestCaseA();
            var results = network.Classify(test).ToArray();
        }
    }
}