using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IRawClassifierTrainingContext<TParameters>
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
        void PruneInputs(params int[] inputIndexes);

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
        /// Gets the classifier
        /// </summary>
        IBinaryPersistable Output { get; }

        /// <summary>
        /// Trains the classifier, associating the output vector with the input vector
        /// </summary>
        /// <param name="outputVector">The desired output vector</param>
        /// <param name="sampleVector">The input vector</param>
        /// <returns>The error</returns>
        double Train(IVector input, IVector output);

        /// <summary>
        /// Trains the classifier using a batch of data
        /// </summary>
        /// <param name="trainingData">A batch of training data</param>
        /// <param name="haltingFunction">A function which returns true to halt the training</param>
        /// <returns></returns>
        double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction);
    }
}