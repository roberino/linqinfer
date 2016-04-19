using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class ClassificationPipeline<TClass, TInput, TVector> where TVector : struct
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

        public virtual void Train(IQueryable<TInput> trainingData, Func<TInput, TClass> classf)
        {
            foreach (var batch in trainingData.Chunk())
            {
                foreach (var value in batch)
                {
                    _learning.Train(classf(value), _featureExtract.ExtractVector(value));
                }
            }
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            return _classifier.Classify(_featureExtract.ExtractVector(obj));
        }

        public IEnumerable<ClassifyResult<TClass>> FindPossibleMatches(TInput obj)
        {
            return _classifier.FindPossibleMatches(_featureExtract.ExtractVector(obj));
        }
    }
}