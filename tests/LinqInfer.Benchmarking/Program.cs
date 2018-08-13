using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace LinqInfer.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            TestNetManual();

            // var report1 = BenchmarkRunner.Run<MatrixBenchmarks>();
            var report2 = BenchmarkRunner.Run<MultilayerNetworkBenchmarks>();

            //foreach(var result in report1.Reports.Concat(report2.Reports))
            //{
            //    Console.WriteLine(result.GenerateResult.ArtifactsPaths.RootArtifactsFolderPath);
            //}
        }

        static void TestMatrixManual()
        {
            var test = new MatrixBenchmarks()
            {
                Height = 500,
                Width = 100
            };

            test.Setup();
            test.Matrix_Multiply();
        }

        static void TestNetManual()
        {
            var test = new MultilayerNetworkBenchmarks()
            {
                Activator = "Sigmoid",
                LayerSize = 8,
                NumberOfHiddenLayers = 1
            };

            test.Setup();
            test.AttachMultilayerNetworkClassifier_Run();
        }
    }
}