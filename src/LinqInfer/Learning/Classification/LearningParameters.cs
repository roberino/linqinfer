using LinqInfer.Data;
using LinqInfer.Utility;
using System;

namespace LinqInfer.Learning.Classification
{
    public class LearningParameters : ICloneableObject<LearningParameters>
    {
        internal const double DefaultLearningRate = 0.1f;

        private Func<LearningParameters, TrainingStatus, bool> _haltingFunction;

        public LearningParameters()
        {
            SetDefaultHaltingFunction();
        }

        /// <summary>
        /// Gets or sets the learning rate
        /// </summary>
        public double LearningRate { get; set; } = DefaultLearningRate;

        /// <summary>
        /// Gets or sets the minimum error
        /// </summary>
        public double MinimumError { get; set; } = 0.005f;

        /// <summary>
        /// Used by some algorithms to control the pace of change
        /// </summary>
        public double Momentum { get; set; } = 0.05;

        /// <summary>
        /// Gets or sets the maximum iterations (used by the standard halting function)
        /// </summary>
        public int? MaxIterations { get; set; }

        /// <summary>
        /// Gets or sets the size of the error history to retain
        /// </summary>
        public int ErrorHistoryCount { get; set; } = 3;

        /// <summary>
        /// Prevents the process from halting
        /// </summary>
        public void NeverHalt()
        {
            HaltingFunction = (x, s) => false;
        }

        public Func<LearningParameters, TrainingStatus, bool> HaltingFunction
        {
            get { return _haltingFunction; }
            set
            {
                if (value == null)
                {
                    SetDefaultHaltingFunction();
                }
                else
                {
                    _haltingFunction = value;
                }
            }
        }

        public virtual LearningParameters Clone(bool deep)
        {
            return CloneInto(new LearningParameters());
        }

        internal T CloneInto<T>(T newParameters)
            where T : LearningParameters
        {
            newParameters.HaltingFunction = HaltingFunction;
            newParameters.LearningRate = LearningRate;
            newParameters.MaxIterations = MaxIterations;
            newParameters.MinimumError = MinimumError;
            newParameters.Momentum = Momentum;

            return newParameters;
        }

        public bool EvaluateHaltingFunction(TrainingStatus status)
        {
            return _haltingFunction(this, status);
        }

        public bool EvaluateHaltingFunction(int iteration, double error)
        {
            return _haltingFunction(this, new TrainingStatus() {AverageError = error, Iteration = iteration});
        }

        internal virtual void Validate()
        {
            ArgAssert.AssertGreaterThanZero(Momentum, nameof(Momentum));
            ArgAssert.AssertGreaterThanZero(MinimumError, nameof(MinimumError));
            ArgAssert.AssertGreaterThanZero(LearningRate, nameof(LearningRate));
        }

        private void SetDefaultHaltingFunction()
        {
            _haltingFunction = (p, s) => (p.MaxIterations.HasValue && s.Iteration >= p.MaxIterations.Value) || s.AverageError <= p.MinimumError;
        }
    }
}