using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public struct TrainingPair<I, C>
    {
        public TrainingPair(I input, C targetOutput)
        {
            Input = input;
            TargetOutput = targetOutput;
        }

        public I Input { get; }

        public C TargetOutput { get; }

        public static implicit operator TrainingPair<I, C>(KeyValuePair<I, C> kv)
        {
            return new TrainingPair<I, C>(kv.Key, kv.Value);
        }
    }
}