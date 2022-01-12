using System;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;

namespace LinqInfer.Learning
{
    public interface INetworkFactory<TInput>
    {
        INetworkClassifier<TClass, TInput> CreateConvolutionalNetwork<TClass>(
            int maxOutputs,
            int? hiddenLayerSize = null, 
            Action<LearningParameters> learningConfig = null)
            where TClass : IEquatable<TClass>;
    }
}