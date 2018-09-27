using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IPropagatedOutput
    {
        /// <summary>
        /// Gets the last output as a vector
        /// </summary>
        IVector Output { get; }
    }
}
