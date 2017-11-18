using LinqInfer.Data;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents the context information required
    /// for training a classifier.
    /// </summary>
    /// <typeparam name="TParameters">The parameters used to create the classifier</typeparam>
    public interface IClassifierTrainingContext<TParameters> : IRawClassifierTrainingContext<TParameters>, ICloneableObject<IClassifierTrainingContext<TParameters>>
    {
    }
}