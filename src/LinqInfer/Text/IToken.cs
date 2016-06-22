namespace LinqInfer.Text
{
    public interface IToken
    {
        int Index { get; }
        TokenType Type { get; }
        string Text { get; }
    }
}