using LinqInfer.Data;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Classification
{
    public interface IPrunableObjectClassifier<TClass, TInput> : 
        ICloneableObject<IPrunableObjectClassifier<TClass, TInput>>, 
        IObjectClassifier<TClass, TInput>, 
        IBinaryPersistable
    {
        void PruneFeatures(params int[] featureIndexes);
    }
}