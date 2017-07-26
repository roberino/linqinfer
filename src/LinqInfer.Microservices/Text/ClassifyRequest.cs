namespace LinqInfer.Microservices.Text
{
    public class ClassifyRequest
    {
        public string IndexName { get; set; }

        public string ClassifierName { get; set; }

        public string Text { get; set; }
    }
}