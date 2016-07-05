using System.Collections.Generic;

namespace LinqInfer.Text
{
    public interface ITokeniser
    {
        IEnumerable<IToken> Tokenise(string body);
    }
}
