namespace LinqInfer.Learning.Classification
{
    public interface IClassifierTrainer : IAssistedLearningProcessor
    {
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