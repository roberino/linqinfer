using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IObjectClassifier<TClass, TInput>
    {
        IEnumerable<ClassifyResult<TClass>> Classify(TInput input);
    }
}
