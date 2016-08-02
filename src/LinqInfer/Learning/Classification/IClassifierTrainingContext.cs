using LinqInfer.Data;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    public interface IClassifierTrainingContext<TClass, TParameters> : IAssistedLearning<TClass, double>, ICloneableObject<IClassifierTrainingContext<TClass, TParameters>>
    {
        /// <summary>
        /// Gets a localised id for the training context
        /// </summary>
        int Id { get; }

        /// <summary>
        /// A counter that can be incremented to track iterations for this context
        /// </summary>
        int IterationCounter { get; set; }

        /// <summary>
        /// Returns the parameters used by this training instance
        /// </summary>
        TParameters Parameters { get; }

        /// <summary>
        /// Removes inputs from the classifier. 
        /// Subsequent training and classification should also
        /// comply with the new input size.
        /// </summary>
        /// <param name="inputIndexes"></param>
        void PruneInputs(params int[] inputIndexes);

        /// <summary>
        /// Gets the current classifier
        /// </summary>
        IFloatingPointClassifier<TClass> Classifier { get; }

        /// <summary>
        /// Returns the current rate of error change.
        /// </summary>
        double? RateOfErrorChange { get; }

        /// <summary>
        /// Returns the error accumulated from training
        /// </summary>
        double? CumulativeError { get; }

        /// <summary>
        /// Returns the average error from training
        /// </summary>
        double? AverageError { get; }

        /// <summary>
        /// Resets the error back to null
        /// </summary>
        void ResetError();

        /// <summary>
        /// Trains the classifier, associating the class and sample vector.
        /// </summary>
        double Train(TClass item, ColumnVector1D sample);
    }
}
