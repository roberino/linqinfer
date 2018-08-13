using LinqInfer.Learning.Features;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    class ObjectClassifier<TClass, TInput, TVector> : IObjectClassifier<TClass, TInput> where TVector : struct
    {
        readonly IClassifier<TClass, TVector> _classifier;
        protected readonly IFeatureExtractor<TInput, TVector> _featureExtract;

        public ObjectClassifier(
            IClassifier<TClass, TVector> classifier,
            IFeatureExtractor<TInput, TVector> featureExtract)
        {
            _classifier = classifier;
            _featureExtract = featureExtract;
        }

        public ClassifyResult<TClass> ClassifyAsBestMatch(TInput obj)
        {
            return _classifier.ClassifyAsBestMatch(_featureExtract.ExtractVector(obj));
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            return _classifier.Classify(_featureExtract.ExtractVector(obj));
        }

        public override string ToString()
        {
            return string.Format("Classifier:{0}=>{1}", typeof(TInput).Name, _classifier);
        }
    }
}