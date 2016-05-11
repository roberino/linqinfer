using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IClassifier<TClass, TVector>
    {
        ClassifyResult<TClass> Classify(TVector[] vector);

        IEnumerable<ClassifyResult<TClass>> FindPossibleMatches(TVector[] vector);
    }
}
