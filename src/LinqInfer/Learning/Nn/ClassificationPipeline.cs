using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        public virtual double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            var classf = classifyingExpression.Compile();

            double error = 0;
            int counter = 0;

            foreach (var batch in trainingData.Chunk())
            {
                foreach (var value in batch)
                {
                    error += _learning.Train(classf(value), _featureExtract.ExtractVector(value));
                    counter++;
                }
            }

            return error / (double)counter;
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