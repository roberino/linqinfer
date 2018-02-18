using LinqInfer.Maths;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    /// <summary>
    /// Represents a layered architecture of neurons which
    /// evaluate and input vector producing an output.
    /// </summary>
    public interface INeuralNetwork
    {
        /// <summary>
        /// The parameters which define the network layout and behaviour
        /// </summary>
        NetworkParameters Parameters { get; }

        /// <summary>
        /// When invoked, the method evaluates the vector, passing it through it's topology and returning an output vector.
        /// </summary>
        ColumnVector1D Evaluate(ColumnVector1D input);
        
        /// <summary>
        /// Returns an enumeration of layers, including the output layer. The input layer is typically not included.
        /// </summary>
        IEnumerable<ILayer> Layers { get; }

        /// <summary>
        /// Removes all associated inputs by index by removing the related weights from each neuron
        /// </summary>
        void PruneInputs(params int[] inputIndexes);
    }
}
