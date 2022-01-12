namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface INetworkModel : IVectorClassifier
    {        
        NetworkSpecification Specification { get; }
    }
}