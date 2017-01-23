using LinqInfer.Data;
using System;
using System.Collections.Generic;

namespace LinqInfer.Maths.Probability
{
    public interface IDiscreteMarkovChain<T> : ITransitionSimulator<T>, IXmlExportable where T : IEquatable<T>
    {
        int Order { get; }
        void AnalyseSequence(IEnumerable<T> sequence);
        void AnalyseSequences<S>(IEnumerable<S> sequences) where S : IEnumerable<T>;
        IDictionary<T, int> GetPriorFrequencies(T currentState);
        IDictionary<T, int> GetFrequencies(T eventValue);
        IDictionary<T, int> GetFrequencies(IEnumerable<T> eventSequence);
        Fraction ProbabilityOfEvent(IEnumerable<T> eventSequence, T nextEventValue);
        void Merge(IDiscreteMarkovChain<T> other);
        void Prune(int maxElements);
    }
}
