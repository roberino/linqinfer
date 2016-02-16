namespace LinqInfer.Learning
{
    public class ClassifyResult<T>
    {
        public T ClassType { get; set; }

        public double Score { get; set; }
    }
}
