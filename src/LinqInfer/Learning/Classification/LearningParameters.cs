namespace LinqInfer.Learning.Classification
{
    public class LearningParameters
    {
        /// <summary>
        /// Gets or sets the learning rate
        /// </summary>
        public double LearningRate { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the minimum error
        /// </summary>
        public double MinimumError { get; set; } = 0.005f;
    }
}