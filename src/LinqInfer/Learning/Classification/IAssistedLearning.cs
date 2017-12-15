namespace LinqInfer.Learning.Classification
{
    public interface IAssistedLearningProcessor<TClass, TVector>
    {
        /// <summary>
        /// Trains the classifier by associating the sample class with the sample vector.
        /// </summary>
        double Train(TClass sampleClass, TVector[] sample);
    }
}