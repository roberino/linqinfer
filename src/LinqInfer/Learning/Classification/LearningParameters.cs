using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Learning.Classification
{
    public class LearningParameters
    {
        internal const double DefaultLearningRate = 0.1f;

        /// <summary>
        /// Gets or sets the learning rate
        /// </summary>
        public double LearningRate { get; set; } = DefaultLearningRate;

        /// <summary>
        /// Used by some algorithms to control the pace of change
        /// </summary>
        public double Momentum { get; set; } = 0.05;
    }
}