using LinqInfer.Learning.Features;
using LinqInfer.Text;
using System;
using System.Text;
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
                    case "norm":
                        pipeline.NormaliseData();
                        break;
                    case "none":
                        break;
                    default:
                        throw new NotSupportedException(op.OperationName);
                }
            }

            return Task.FromResult(pipeline);
        }

        public string Hash
        {
            get
            {
                var sb = new StringBuilder();

                var bytes = Encoding.ASCII.GetBytes($"{IndexName}/{Transform}/{MaxVectorSize}");

                foreach (byte b in bytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }

                return sb.ToString();
            }
        }
    }
}