namespace LinqInfer.Learning.Classification
{
    public interface INetworkClassifier<TClass, TInput> : IDynamicClassifier<TClass, TInput>, IHasNetworkTopology
    {
    }
}