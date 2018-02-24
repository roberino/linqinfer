﻿using LinqInfer.Data;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents the context information required
    /// for training a classifier.
    /// </summary>
    /// <typeparam name="TParameters">The parameters used to create the classifier</typeparam>
    public interface IClassifierTrainingContext<TParameters> : IClassifierTrainer, ICloneableObject<IClassifierTrainingContext<TParameters>>
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
    }
}