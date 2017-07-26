namespace LinqInfer.Microservices.Text
{
    public class ClassifierRequest : FeatureExtractRequest
    {
        public float? ErrorTolerance { get; set; }
        public string ClassifierName { get; set; }
        public string ClassAttributeName { get; set; }
    }
}