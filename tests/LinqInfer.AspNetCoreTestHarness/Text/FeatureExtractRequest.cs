﻿using LinqInfer.Learning.Features;
using LinqInfer.Text;
using System.Threading.Tasks;

namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class FeatureExtractRequest
    {
        public string IndexName { get; set; }

        public string Filter { get; set; }

        public int MaxVectorSize { get; set; }

        public Task<FeatureProcessingPipeline<TokenisedTextDocument>> Apply(FeatureProcessingPipeline<TokenisedTextDocument> pipeline)
        {
            var parser = new FilterParser();

            foreach(var op in parser.Parse(Filter))
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