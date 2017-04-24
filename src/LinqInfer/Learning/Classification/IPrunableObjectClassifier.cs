using LinqInfer.Data;

namespace LinqInfer.Learning.Classification
{
    public interface IPrunableObjectClassifier<TClass, TInput> : 
        ICloneableObject<IPrunableObjectClassifier<TClass, TInput>>, 
        IObjectClassifier<TClass, TInput>, 
        IBinaryPersistable
        // IExportableAsVectorDocument
    {
        void PruneFeatures(params int[] featureIndexes);
    }
}