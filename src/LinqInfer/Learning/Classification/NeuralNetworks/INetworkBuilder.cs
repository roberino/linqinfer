namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface INetworkBuilder
    {
        IClassifierTrainingContext<INetworkModel> Build();
    }
}