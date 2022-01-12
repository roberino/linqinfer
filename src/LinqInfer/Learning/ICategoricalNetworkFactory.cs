using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification.NeuralNetworks;

namespace LinqInfer.Learning
{
    public interface ICategoricalNetworkFactory<TInput> : INetworkFactory<TInput>
    {
        ITimeSequenceAnalyser<TInput> CreateTimeSequenceAnalyser(PortableDataDocument data);

        ITimeSequenceAnalyser<TInput> CreateTimeSequenceAnalyser();
    }
}