using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqInfer.Text
{
    public class WordTokeniser : ITokeniser
    {
        private readonly Regex _wordRegex = new Regex("[a-z0-9'’-]+", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public IEnumerable<string> Tokenise(string body)
        {
            if (body == null) return Enumerable.Empty<string>();

            return _wordRegex
                .Matches(body)
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => w.Any(c => char.IsLetterOrDigit(c)))
                .Select(w => w.ToLowerInvariant().Trim());
        }
    }
}
