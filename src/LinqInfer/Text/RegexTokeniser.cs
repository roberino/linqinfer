using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqInfer.Text
{
    public class RegexTokeniser : ITokeniser
    {
        private static readonly Regex _defaultRegex = new Regex("[a-z0-9'’-]+", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _wordRegex;

        public RegexTokeniser(string inputPattern = null, RegexOptions options = RegexOptions.None)
        {
            _wordRegex = inputPattern == null ? _defaultRegex : new Regex(inputPattern, options);
        }

        public IEnumerable<IToken> Tokenise(string body)
        {
            if (body == null) return Enumerable.Empty<IToken>();

            return _wordRegex
                .Matches(body)
                .Cast<Match>()
                .Where(t => t.Value.Any(c => char.IsLetterOrDigit(c)))
                .Select(w => new Token(w.Value.ToLowerInvariant().Trim(), w.Index, TokenType.Word));
        }
    }
}
