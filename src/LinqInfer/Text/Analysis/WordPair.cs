namespace LinqInfer.Text.Analysis
{
    public sealed class WordPair
    {
        public WordPair(string word = null)
        {
            WordA = word;
        }

        public string WordA { get; internal set; }
        public string WordB { get; internal set; }
    }
}