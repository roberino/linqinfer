using LinqInfer.Data.Pipes;
using LinqInfer.Maths.Probability;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class CategoricalFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
    {
        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && propertyExtractor.FeatureMetadata.Model == DistributionModel.Categorical;
        }

        public override async Task<IFloatingPointFeatureExtractor<T>> BuildAsync(IAsyncEnumerator<T> samples)
        {
            var vals = new Dictionary<string, long>();

            await samples.ProcessUsing(b =>
            {
                foreach (var v in b.Items)
                {
                    vals[GetValue(v)] = 1;
                }
            }, CancellationToken.None);

            var encoder = new OneHotEncoding<string>(new HashSet<string>(vals.Keys));

            var fe = new CategoricalFeatureExtractor<T, string>(GetValue, Feature.CreateDefaults(Properties.Select(p => p.Property.Name), DistributionModel.Categorical), new HashSet<string>(vals.Keys));

            return fe;
        }

        private string GetValue(T item)
        {
            return Properties
                .Select(p => p.Property.GetValue(item))
                .Aggregate(new StringBuilder(), (s, v) => (s.Length > 0 ? s.Append('/') : s).Append(Convert(v)))
                .ToString();
        }

        private string Convert(object value)
        {
            if (value == null) return string.Empty;

            return value.ToString();
        }
    }
}