namespace LinqInfer.Text.Analysis
{
    public sealed class WordContext
    {
        public IToken[] ContextualWords { get; internal set; }

        public IToken TargetWord { get; internal set; }
    }
}
