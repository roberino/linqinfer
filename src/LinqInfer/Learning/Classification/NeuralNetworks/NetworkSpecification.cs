using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public class NetworkSpecification
    {
        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, int outputVectorSize, params LayerSpecification[] layers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));
            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            OutputVectorSize = outputVectorSize;
            Layers = layers.ToList();
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }
        public int OutputVectorSize { get; }

        public IList<LayerSpecification> Layers { get; }
    }
}