using BenchmarkDotNet.Running;
using System;

namespace LinqInfer.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var report1 = BenchmarkRunner.Run<MatrixBenchmarks>();
            var report2 = BenchmarkRunner.Run<MultilayerNetworkBenchmarks>();

            foreach(var result in report1.Reports)
            {
                Console.WriteLine(result.GenerateResult.ArtifactsPaths);
            }
        }

        private static void TestManual()
        {
            var test = new MatrixBenchmarks()
            {
                Height = 500,
                Width = 100
            };

            test.Setup();
            test.Matrix_Multiply();
        }
    }
}