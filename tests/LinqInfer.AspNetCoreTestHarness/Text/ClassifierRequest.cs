namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class ClassifierRequest : RequestBase
    {
        public string IndexName { get; set; }

        public string ClassifierName { get; set; }

        public int MaxVectorSize { get; set; }
    }
}