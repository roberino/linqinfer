﻿using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    /// <summary>
    /// Represents a layer in a layered network of neurons
    /// </summary>
    public interface ILayer : INetworkSignalFilter, IPropagatedOutput, ICloneableObject<ILayer>
    {
        /// <summary>
        /// The size of the vector which the layer can receive
        /// </summary>
        int InputVectorSize { get; }

        /// <summary>
        /// The size of the layer (i.e. number of neurons)
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the activator used for the layer
        /// </summary>
        ActivatorExpression Activator { get; }

        /// <summary>
        /// Gets the function used to calculate errors
        /// </summary>
        ILossFunction LossFunction { get; }

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
        /// Applies a function over each neuron supplying the neuron as a parameter
        /// </summary>
        IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func);

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
        /// Exports the raw data and properties
        /// </summary>
        PortableDataDocument Export();

        /// <summary>
        /// Exports the raw data
        /// </summary>
        Matrix ExportData();

        /// <summary>
        /// Imports raw data
        /// </summary>
        void Import(PortableDataDocument data);
    }
}