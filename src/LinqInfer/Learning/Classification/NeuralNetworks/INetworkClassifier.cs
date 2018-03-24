using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface INetworkClassifier<TClass, TInput> : 
        IDynamicClassifier<TClass, TInput>, 
        IHasNetworkTopology,
        IHasSerialisableTransformation
    {
    }
}