using System;

namespace LinqInfer.Genetics
{
    public abstract class MutatableParameter
    {
        public int MutationCounter { get; private set; }

        public object CurrentValue { get; protected set; }

        public abstract object OptimalValue { get; }

        public abstract double? ValueFitnessScoreCovariance { get; }

        public abstract TypeCode Type { get; }

        public virtual void Reset()
        {
            MutationCounter = 0;
            WasMutated = false;
        }

        public void Score(double fitnessScore)
        {
            WasMutated = false;
            SubmitScore(fitnessScore);
        }

        public bool WasMutated { get; private set; }

        protected abstract object MutateValue();

        protected abstract void SubmitScore(double fitnessScore);

        public void Mutate()
        {
            CurrentValue = MutateValue();
            MutationCounter++;
            WasMutated = true;
        } 
    }
}