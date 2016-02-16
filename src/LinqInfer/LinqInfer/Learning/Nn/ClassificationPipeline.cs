using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public class ClassificationPipeline<TClass, TInput, TVector> where TVector : struct
    {
        private readonly IAssistedLearning<TClass, TVector> _learning;
        private readonly IClassifier<TClass, TVector> _classifier;
        private readonly IFeatureExtractor<TInput, TVector> _featureExtract;

        public ClassificationPipeline(IAssistedLearning<TClass, TVector> learning, 
            IClassifier<TClass, TVector> classifier, 
            IFeatureExtractor<TInput, TVector> featureExtract,
            TInput normalisingSample = default(TInput))
        {
            _learning = learning;
            _classifier = classifier;
            _featureExtract = featureExtract;

            _featureExtract.CreateNormalisingVector(normalisingSample);
        }

        public void Train(IEnumerable<Tuple<TClass, TInput>> trainingData)
        {
            foreach(var item in trainingData)
            {
                _learning.Train(item.Item1, _featureExtract.ExtractVector(item.Item2));
            }
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            return _classifier.Classify(_featureExtract.ExtractVector(obj));
        }
    }
}
