namespace LinqInfer.Learning.Features
{
    public interface IFeatureProcessingPipeline<T> : IFeatureDataSource, IFeatureTransformBuilder<T> where T : class
    {
    }
}
