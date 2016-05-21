using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IClassifier<TClass, TVector> : IObjectClassifier<TClass, TVector[]>
    {
        ClassifyResult<TClass> ClassifyAsBestMatch(TVector[] vector);
    }
}
