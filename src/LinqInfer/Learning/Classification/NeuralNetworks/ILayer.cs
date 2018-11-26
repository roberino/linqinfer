using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    /// <summary>
    /// Represents a layer in a layered network of neurons
    /// </summary>
    public interface ILayer : INetworkSignalFilter
    {
        /// <summary>
        /// The size of the layer (i.e. number of neurons)
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the activator used for the layer
        /// </summary>
        ActivatorExpression Activator { get; }
        
        /// <summary>
        /// Gets the function for updating weights
        /// </summary>
        WeightUpdateRule WeightUpdateRule { get; }

        /// <summary>
        /// Gets a neuron by index
        /// </summary>
        /// <param name="index">The zero base index</param>
        INeuron this[int index] { get; }

        /// <summary>
        /// Applies a function over each neuron supplying the neuron and the index as a parameter
        /// </summary>
        ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func);

        /// <summary>
        /// Expands the layer by adding new neurons
        /// </summary>
        void Grow(int numberOfNewNeurons = 1);

        /// <summary>
        /// Prunes the layer by removing neurons
        /// </summary>
        /// <param name="predicate">A predicate to determine which neurons to remove</param>
        void Prune(Func<INeuron, bool> predicate);

        /// <summary>
        /// Exports the raw data
        /// </summary>
        Matrix ExportWeights();
    }
}