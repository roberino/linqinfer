namespace LinqInfer.Text.Analysis
{
    public sealed class SyntacticContext
    {
        public IToken[] ContextualWords { get; internal set; }

        public IToken TargetWord { get; internal set; }
    }
}
