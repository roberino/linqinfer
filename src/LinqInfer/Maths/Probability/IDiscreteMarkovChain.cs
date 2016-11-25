﻿using System;
using System.Collections.Generic;

namespace LinqInfer.Maths.Probability
{
    public interface IDiscreteMarkovChain<T> : ITransitionSimulator<T> where T : IEquatable<T>
    {
        int Order { get; }
        void AnalyseSequence(IEnumerable<T> sequence);
        void AnalyseSequences<S>(IEnumerable<S> sequences) where S : IEnumerable<T>;
        IDictionary<T, int> GetFrequencies(T eventValue);
        IDictionary<T, int> GetFrequencies(IEnumerable<T> eventSequence);
        Fraction ProbabilityOfEvent(IEnumerable<T> eventSequence, T nextEventValue);
    }
}
