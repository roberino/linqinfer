using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IClassifier<TClass, TVector>
    {
        ClassifyResult<TClass> ClassifyAsBestMatch(TVector[] vector);

        IEnumerable<ClassifyResult<TClass>> Classify(TVector[] vector);
    }
}
