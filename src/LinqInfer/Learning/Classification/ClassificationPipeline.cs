using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Classification
{
    [DebuggerDisplay("Error:{Error}")]
    internal class ClassificationPipeline<TClass, TInput, TVector> : ObjectClassifier<TClass, TInput, TVector> where TVector : struct
    {
        private readonly IAssistedLearning<TClass, TVector> _learning;

        private double? error;

        public ClassificationPipeline(IAssistedLearning<TClass, TVector> learning,
            IClassifier<TClass, TVector> classifier,
            IFeatureExtractor<TInput, TVector> featureExtract,
            TInput normalisingSample = default(TInput),
            bool normalise = true) : base(classifier, featureExtract)
        {
            _learning = learning;

            if (normalise)
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

            return error.GetValueOrDefault() / counter;
        }

        public void ResetError()
        {
            error = 0;
        }

        public double? Error
        {
            get
            {
                return error;
            }
        }

        public virtual double Train(TInput value, Func<TInput, TClass> classf)
        {
            if (!error.HasValue) error = 0;
             var e = _learning.Train(classf(value), _featureExtract.ExtractVector(value));
            error += e;
            return e;
        }
    }
}