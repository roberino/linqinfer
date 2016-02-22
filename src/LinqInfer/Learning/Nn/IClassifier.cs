using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public interface IClassifier<TClass, TVector>
    {
        ClassifyResult<TClass> Classify(TVector[] vector);

        IEnumerable<ClassifyResult<TClass>> FindPossibleMatches(TVector[] vector);
    }
}
