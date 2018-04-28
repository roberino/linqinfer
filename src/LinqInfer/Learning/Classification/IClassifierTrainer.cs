namespace LinqInfer.Learning.Classification
{
    public interface IClassifierTrainer : IAssistedLearningProcessor
    {
        /// <summary>
        /// Removes inputs from the classifier. 
        /// Subsequent training and classification should also
        /// comply with the new input size.
        /// </summary>
        void PruneInputs(params int[] inputIndexes);

        /// <summary>
        /// Returns the average error from training
        /// </summary>
        double? AverageError { get; }

        /// <summary>
        /// Resets the error back to null
        /// </summary>
        void ResetError();

        /// <summary>
        /// Gets the classifier
        /// </summary>
        IVectorClassifier Output { get; }
    }
}