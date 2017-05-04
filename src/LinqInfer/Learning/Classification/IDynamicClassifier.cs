using LinqInfer.Data;

namespace LinqInfer.Learning.Classification
{
    public interface IDynamicClassifier<TClass, TInput> : IPrunableObjectClassifier<TClass, TInput>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        /// <summary>
        /// Returns classifier statistics
        /// </summary>
        ClassifierStats Statistics { get; }

        /// <summary>
        /// Adds a new training sample to the classifier
        /// </summary>
        /// <param name="obj">The object being classified</param>
        /// <param name="classification">The classification type</param>
        void Train(TInput obj, TClass classification);
    }
}