using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Running;
using LinqInfer.Maths;

namespace LinqInfer.Benchmarking
{
    [HtmlExporter]
    [CsvExporter]
    [MarkdownExporter]
    public class MatrixBenchmarks
    {
        Matrix _matrix1;
        Matrix _matrix2;

        [Params(5, 100, 500)]
        public int Width { get; set; }

        [Params(5, 100, 100)]
        public int Height { get; set; }

        [IterationSetup]
        public void Setup()
        {
            _matrix1 = Matrix.RandomMatrix(Width, Height, new Range(100, -100));
            _matrix2 = Matrix.RandomMatrix(Height, Width, new Range(100, -100));
        }

        [Benchmark]
        public void Matrix_Multiply()
        {
            var matrix3 = _matrix1 * _matrix2;
        }

        [Benchmark]
        public void Matrix_Addition()
        {
            var matrix3 = _matrix1 + _matrix1;
        }

        [Benchmark]
        public void Matrix_CovarianceMatrix()
        {
            var covar = _matrix1.CovarianceMatrix;
        }
    }
}