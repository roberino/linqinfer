using LinqInfer.Learning.Classification.NeuralNetworks;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    class ClassifierCluster<TClass, TInput>
        where TClass : IEquatable<TClass>
    {
        IList<(INetworkClassifier<TClass, TInput> classifier, Func<TInput[], bool>)> classifiers;

        public ClassifierCluster(int maxSize)
        {
            classifiers = new List<(INetworkClassifier<TClass, TInput> classifier, Func<TInput[], bool>)>();
        }

        public void Train(TInput[] trainingData)
        {

        }
    }
}