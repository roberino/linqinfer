using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    [DebuggerDisplay("Error:{Error}")]
    internal class ClassificationPipeline<TClass, TInput, TVector> where TVector : struct
    {
        private readonly IAssistedLearning<TClass, TVector> _learning;
        private readonly IClassifier<TClass, TVector> _classifier;
        private readonly IFeatureExtractor<TInput, TVector> _featureExtract;

        private double error;

        public ClassificationPipeline(IAssistedLearning<TClass, TVector> learning,
            IClassifier<TClass, TVector> classifier,
            IFeatureExtractor<TInput, TVector> featureExtract,
            TInput normalisingSample = default(TInput),
            bool normalise = true)
        {
            _learning = learning;
            _classifier = classifier;
            _featureExtract = featureExtract;

            if(normalise)
                _featureExtract.CreateNormalisingVector(normalisingSample);
        }

        public virtual double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            var classf = classifyingExpression.Compile();

            int counter = 0;

            foreach (var batch in trainingData.Chunk())
            {
                foreach (var value in batch)
                {
                    Train(value, classf);
                    counter++;
                }
            }

            return error / counter;
        }

        public void ResetError()
        {
            error = 0;
        }

        public double Error
        {
            get
            {
                return error;
            }
        }

        public virtual double Train(TInput value, Func<TInput, TClass> classf)
        {
            var e = _learning.Train(classf(value), _featureExtract.ExtractVector(value));
            error += e;
            return e;
        }

        public ClassifyResult<TClass> Classify(TInput obj)
        {
            return _classifier.Classify(_featureExtract.ExtractVector(obj));
        }

        public IEnumerable<ClassifyResult<TClass>> FindPossibleMatches(TInput obj)
        {
            return _classifier.FindPossibleMatches(_featureExtract.ExtractVector(obj));
        }

        public override string ToString()
        {
            return string.Format("Classifier:{0}=>{1}", typeof(TInput).Name, _classifier);
        }
    }
}