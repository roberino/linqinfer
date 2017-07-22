namespace LinqInfer.Microservices.Text
{
    public class ClassifierRequest : FeatureExtractRequest
    {
        public string ClassifierName { get; set; }

        public string ClassAttributeName { get; set; }
    }
}