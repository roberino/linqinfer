using System;

namespace LinqInfer.Storage
{
    [Serializable]
    public class SampleSummary
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double StdDev { get; set; }
        public double Mean { get; set; }
        public int Count { get; set; }
        public int Size { get; set; }
    }
}
