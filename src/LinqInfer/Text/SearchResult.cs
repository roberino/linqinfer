using LinqInfer.Learning.Classification;

namespace LinqInfer.Text
{
    public class SearchResult : ClassifyResult<string>
    {
        public string DocumentKey { get { return ClassType; } }
    }
}
