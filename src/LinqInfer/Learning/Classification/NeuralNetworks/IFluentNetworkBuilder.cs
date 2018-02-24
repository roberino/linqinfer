using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder AddHiddenLayer(LayerSpecification layer);
        IFluentNetworkBuilder AddHiddenSigmoidLayer(int layerSize);
        IFluentNetworkBuilder ConfigureLearningParameters(double learningRate, double minimumError);
        IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters);
        IFluentNetworkBuilder ConfigureOutputLayer(ActivatorFunc activator, ILossFunction lossFunction, Range? initialWeightRange = null);
    }
}