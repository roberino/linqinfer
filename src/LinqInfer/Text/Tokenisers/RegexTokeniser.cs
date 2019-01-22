using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqInfer.Text.Tokenisers
{
    public class RegexTokeniser : ITokeniser
    {
        static readonly Regex _defaultRegex = new Regex("[a-z0-9'’-]+", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly Regex _wordRegex;

        public RegexTokeniser(string inputPattern = null, RegexOptions options = RegexOptions.None)
        {
            _wordRegex = inputPattern == null ? _defaultRegex : new Regex(inputPattern, options);
        }

        public IEnumerable<IToken> Tokenise(string body, int? indexOffset = null)
        {
            if (body == null) return Enumerable.Empty<IToken>();

            var offset = indexOffset.GetValueOrDefault();

            return _wordRegex
                .Matches(body)
                .Cast<Match>()
                .Where(t => t.Value.Any(c => char.IsLetterOrDigit(c)))
                .Select(w => new Token(w.Value.ToLowerInvariant().Trim(), w.Index + offset, TokenType.Word));
        }
    }
}
