using LinqInfer.Learning.Features;
using LinqInfer.Text;
using System.Threading.Tasks;

namespace LinqInfer.Microservices.Text
{
    public class FeatureExtractRequest : RequestBase
    {
        public string IndexName { get; set; }

        public string Transform { get; set; }

        public int MaxVectorSize { get; set; }

        public Task<FeatureProcessingPipeline<TokenisedTextDocument>> Apply(FeatureProcessingPipeline<TokenisedTextDocument> pipeline)
        {
            var parser = new FilterParser();

            foreach(var op in parser.Parse(Transform))
            {
                switch (op.OperationName)
                {
                    case "pca":
                        pipeline.PrincipalComponentReduction(int.Parse(op.Parameters[0]), int.Parse(op.Parameters[1]));
                        break;
                    case "map":
                        pipeline.KohonenSOMFeatureReduction(int.Parse(op.Parameters[0]), int.Parse(op.Parameters[1]));
                        break;
                }
            }

            return Task.FromResult(pipeline);
        }
    }
}