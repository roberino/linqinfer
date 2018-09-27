using System.Collections.Generic;

namespace LinqInfer.Utility.Expressions
{
    class TokenCache
    {
        readonly IDictionary<string, Token> _cache;

        public TokenCache()
        {
            _cache = new Dictionary<string, Token>();
        }

        public Token Get(string value)
        {
            if (!_cache.TryGetValue(value, out var token))
            {
                _cache[value] = token = new Token(value);
            }

            return token;
        }
    }
}
