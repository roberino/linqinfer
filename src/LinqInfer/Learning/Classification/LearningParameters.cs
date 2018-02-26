using System;

namespace LinqInfer.Learning.Classification
{
    public class LearningParameters
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

        public bool EvaluateHaltingFunction(int iteration, double currentError)
        {
            return _haltingFunction(iteration, currentError);
        }

        private void SetDefaultHaltingFunction()
        {
            _haltingFunction = (n, e) => (MaxIterations.HasValue && n >= MaxIterations.Value) || e <= MinimumError;
        }
    }
}