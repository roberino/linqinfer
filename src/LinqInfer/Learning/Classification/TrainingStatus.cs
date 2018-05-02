namespace LinqInfer.Learning.Classification
{
    public class TrainingStatus
    {
        public double AverageError { get; set; }
        public double Trend { get; set; }
        public int TrendHistory { get; set; }
        public long SamplesProcessed { get; set; }
        public long Iteration { get; set; }
    }
}