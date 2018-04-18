using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IWeightUpdateRule
    {
        /// <summary>
        /// Adjusts the learning rate, returning the new rate
        /// </summary>
        /// <param name="rateAdjustment"></param>
        /// <returns></returns>
        double AdjustLearningRate(Func<double, double> rateAdjustment);

        /// <summary>
        /// Executes the weight update rule, returning a new weight value
        /// </summary>
        double Execute(WeightUpdateParameters updateParams);

        /// <summary>
        /// Exports the rule as a string value
        /// </summary>
        string Export();
    }
}