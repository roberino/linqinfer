using System;

namespace LinqInfer.Genetics
{
    public abstract class MutatableParameter
    {
        internal const int BACKLOG_SIZE= 10;

        protected object _currentValue;

        /// <summary>
        /// Counts the number of times the parameter was mutated
        /// </summary>
        public int MutationCounter { get; private set; }

        /// <summary>
        /// Returns true if the value has been access since the last mutation
        /// </summary>
        public bool WasAccessed { get; protected set; }

        /// <summary>
        /// Returns true if all possible values have been used
        /// </summary>
        public abstract bool IsExhausted { get; }

        /// <summary>
        /// Returns the value which produces the highest fitness score
        /// </summary>
        public abstract object OptimalValue { get; }

        /// <summary>
        /// Returns the covariance between the value and the associated fitness score
        /// </summary>
        public abstract double? ValueFitnessScoreCovariance { get; }

        /// <summary>
        /// Returns the type of variable
        /// </summary>
        public abstract TypeCode Type { get; }

        /// <summary>
        /// Gets the current value
        /// </summary>
        public object CurrentValue
        {
            get
            {
                WasAccessed = true;
                return _currentValue;
            }
        }

        /// <summary>
        /// Resets the parameter back to initial values, clearing history
        /// </summary>
        internal virtual void Reset()
        {
            MutationCounter = 0;
            WasMutated = false;
            WasAccessed = false;
            OnReset();
        }

        /// <summary>
        /// Submits the fitness score obtained while using the current mutated (or initial) value
        /// </summary>
        /// <param name="fitnessScore">The fitness score (greater is better)</param>
        internal void Score(double fitnessScore)
        {
            WasMutated = false;
            SubmitScore(fitnessScore);
        }

        /// <summary>
        /// Returns true if a parameter was mutated
        /// </summary>
        internal bool WasMutated { get; private set; }

        protected abstract void OnReset();

        /// <summary>
        /// When implemented, produces a new mutated value
        /// </summary>
        /// <returns></returns>
        protected abstract object MutateValue();

        protected abstract void SubmitScore(double fitnessScore);

        /// <summary>
        /// Mutates the value, attempting to "breed" a more optimal value based on history
        /// </summary>
        internal void Mutate()
        {
            _currentValue = MutateValue();
            MutationCounter++;
            WasMutated = true;
            WasAccessed = false;
        }

        /// <summary>
        /// Changes the value to the current optimal value
        /// </summary>
        internal void Optimise()
        {
            _currentValue = OptimalValue;
            WasMutated = true;
            WasAccessed = false;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} (mutated {2})", Type, CurrentValue, MutationCounter);
        }
    }
}