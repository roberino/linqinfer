using LinqInfer.Maths;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class CalculationCrashDump
    {
        public CalculationCrashDump()
        {
            VectorData = new Dictionary<string, IVector>();
            ScalarData = new Dictionary<string, double>();
        }

        public IDictionary<string, double> ScalarData { get; }
        public IDictionary<string, IVector> VectorData { get; }
    }
}
