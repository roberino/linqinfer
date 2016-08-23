using System;

namespace LinqInfer.Text
{
    public interface IToken : IEquatable<IToken>
    {
        byte Weight { get; }
        int Index { get; }
        TokenType Type { get; }
        string Text { get; }
    }
}