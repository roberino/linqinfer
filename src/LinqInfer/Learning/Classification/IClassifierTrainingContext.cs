using LinqInfer.Data;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents the context information required
    /// for training a classifier.
    /// </summary>
    /// <typeparam name="TClass">The class type</typeparam>
    /// <typeparam name="TParameters">The parameters used to create the classifier</typeparam>
    public interface IClassifierTrainingContext<TClass, TParameters> : IRawClassifierTrainingContext<TParameters>, IAssistedLearning<TClass, double>, ICloneableObject<IClassifierTrainingContext<TClass, TParameters>>
    {
        /// <summary>
        /// Gets the current classifier
        /// </summary>
        IFloatingPointClassifier<TClass> Classifier { get; }

        /// <summary>
        /// Trains the classifier, associating the class and sample vector.
        /// </summary>
        double Train(TClass item, ColumnVector1D sample);
    }
}