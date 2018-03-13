using LinqInfer.Data;
using LinqInfer.Utility;
using System;

namespace LinqInfer.Learning.Classification
{
    public class LearningParameters : ICloneableObject<LearningParameters>
    {
        private Func<int, double, bool> _haltingFunction;

        public LearningParameters()
        {
            SetDefaultHaltingFunction();
        }

        /// <summary>
        /// Gets or sets the learning rate
        /// </summary>
        public double LearningRate { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the minimum error
        /// </summary>
        public double MinimumError { get; set; } = 0.005f;

        /// <summary>
        /// Used by some algorithms to control the pace of change
        /// </summary>
        public double Momentum { get; set; } = 0.05;

        /// <summary>
        /// Gets or sets the maximum iterations
        /// </summary>
        public int? MaxIterations { get; set; }

        public Func<int, double, bool> HaltingFunction
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

        public bool EvaluateHaltingFunction(int iteration, double currentError)
        {
            return _haltingFunction(iteration, currentError);
        }

        internal virtual void Validate()
        {
            ArgAssert.AssertGreaterThanZero(Momentum, nameof(Momentum));
            ArgAssert.AssertGreaterThanZero(MinimumError, nameof(MinimumError));
            ArgAssert.AssertGreaterThanZero(LearningRate, nameof(LearningRate));
        }

        private void SetDefaultHaltingFunction()
        {
            _haltingFunction = (n, e) => (MaxIterations.HasValue && n >= MaxIterations.Value) || e <= MinimumError;
        }
    }
}