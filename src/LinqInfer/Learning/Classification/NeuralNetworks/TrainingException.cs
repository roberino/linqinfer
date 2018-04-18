using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class CalculationException : Exception
    {
        public CalculationException()
        {
            Dump = new CalculationCrashDump();
        }

        public CalculationCrashDump Dump { get; }
    }
}