using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents a class which can assist in the training process
    /// of a classification model
    /// </summary>
    public interface IAssistedLearningProcessor
    {
        /// <summary>
        /// Changes the current learning rate
        /// </summary>
        void AdjustLearningRate(Func<double, double> rateAdjustment);

        /// <summary>
        /// Trains the model using the set of training data
        /// </summary>
        /// <param name="trainingData">A set of training vector inputs and target outputs</param>
        /// <param name="haltingFunction">A function which takes the iteration number, the current error and halts the process if the return value is true</param>
        /// <returns>The final error</returns>
        double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction);

        /// <summary>
        /// Trains using a single input and target output vector
        /// </summary>
        /// <param name="inputVector">The input</param>
        /// <param name="output">The target output</param>
        /// <returns>The final error</returns>
        double Train(IVector inputVector, IVector output);
    }
}