using System.Linq;

namespace LinqInfer.Probability
{
    internal class ContinuousSample<T>
    {
        public ContinuousSample(IQueryable<T> values, IKernelDensityEstimator<T> estimationModel)
        {
        }
    }
}
