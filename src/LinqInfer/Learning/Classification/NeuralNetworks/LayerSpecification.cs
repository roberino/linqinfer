using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class LayerSpecification
    {
        public LayerSpecification(int layerSize, ActivatorFunc activator, Range initialWeightRange)
        {
            ArgAssert.AssertGreaterThanZero(layerSize, nameof(layerSize));
            ArgAssert.AssertNonNull(activator, nameof(activator));

            LayerSize = layerSize;
            Activator = activator;
            InitialWeightRange = initialWeightRange;
            NeuronFactory = x => new NeuronBase(x, InitialWeightRange);
        }

        /// <summary>
        /// The number of neurons in each layer
        /// </summary>
        public int LayerSize { get; }

        /// <summary>
        /// Returns a factory function for creating new neurons
        /// </summary>
        public Func<int, INeuron> NeuronFactory { get; }

        /// <summary>
        /// Gets the activator function
        /// </summary>
        public ActivatorFunc Activator { get; }

        /// <summary>
        /// Gets or sets the initial weight range used to initialise neurons
        /// </summary>
        public Range InitialWeightRange { get; }
    }
}