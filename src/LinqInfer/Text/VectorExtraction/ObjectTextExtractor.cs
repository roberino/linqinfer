using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Text.VectorExtraction
{
    internal class ObjectTextExtractor<T> where T : class
    {
        private readonly Type _type = typeof(T);
        private readonly ITokeniser _tokeniser;

        public ObjectTextExtractor(ITokeniser tokeniser)
        {
            _tokeniser = tokeniser;
        }

        public Func<T, IEnumerable<IToken>> CreateObjectTextTokeniser(string setName = null)
        {
            int i = 0;

            var featureProps = _type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(string))
                .Select(p =>
                {
                    var featureDef = p.GetCustomAttributes<FeatureAttribute>().FirstOrDefault(a => setName == null || a.SetName == setName);

                    return new
                    {
                        property = p,
                        featureDef = featureDef ?? new FeatureAttribute()
                    };
                })
                .Where(f => !f.featureDef.Ignore)
                .OrderBy(f => f.featureDef.IndexOrder)
                .ThenBy(f => f.property.Name)
                .Select(f => new { converter = CreateConverter(f.property, f.featureDef), feature = f, index = i++ })
                .Where(c => c.converter != null)
                .ToList();

            return x =>
            {
                var allTokens = featureProps
                .Select(f => f.converter(x))
                .Where(t => t != null)
                .Select(t => _tokeniser.Tokenise(t));

                var concattedList = new List<IToken>();

                foreach (var tokens in allTokens)
                {
                    concattedList.AddRange(tokens);
                }

                return concattedList;
            };
        }

        private Func<T, string> CreateConverter(PropertyInfo prop, FeatureAttribute featureDef)
        {
            return x =>
            {
                var v = prop.GetValue(x);
                return v == null ? null : v.ToString();
            };
        }
    }
}
