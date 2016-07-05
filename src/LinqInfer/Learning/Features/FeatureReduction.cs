using System.Collections;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class FeatureReduction<T> where T : class
    {
        protected readonly IFeatureProcessingPipeline<T> _featurePipeline;

        public FeatureReduction(IFeatureProcessingPipeline<T> featurePipeline)
        {
            _featurePipeline = featurePipeline;
        }

        public void Reduce()
        {
        }
    }
}
