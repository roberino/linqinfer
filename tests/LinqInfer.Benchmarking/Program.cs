using BenchmarkDotNet.Running;

namespace LinqInfer.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<MatrixBenchmarks>();
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