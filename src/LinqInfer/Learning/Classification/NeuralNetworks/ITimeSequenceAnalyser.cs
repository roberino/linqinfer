using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths.Probability;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface ITimeSequenceAnalyser<TInput> : ITransitionSimulator<TInput>, IExportableAsDataDocument
    {
        void Reset();
        void Train(TInput input);
        void Train(IEnumerable<TInput> sequence);
    }
}