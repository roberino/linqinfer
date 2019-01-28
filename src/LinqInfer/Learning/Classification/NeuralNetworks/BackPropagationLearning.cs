using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class BackPropagationLearning : IAssistedLearningProcessor
    {
        readonly IMultilayerNetwork _network;

        public BackPropagationLearning(IMultilayerNetwork network)
        {
            network.Specification.Validate();

            _network = network;
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _network.ForwardPropagate(f =>
            {
                if (f is ILayer layer)
                {
                    layer.WeightUpdateRule.AdjustLearningRate(rateAdjustment);
                }
            });
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingSet, double errorThreshold = 0)
        {
            return Train(trainingSet, (n, e) => e < errorThreshold);
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction)
        {
            double errTotal = 0;
            double err = 0;
            int c = 0;

            foreach (var inputPair in trainingData)
            {
                errTotal += (err = Train(inputPair.Input, inputPair.TargetOutput));

                if (haltingFunction(c++, err)) break;
            }

            return errTotal;
        }

        public double Train(IVector inputVector, IVector targetOutput)
        {
            var output = _network.Apply(inputVector);

            Validate(output, inputVector, targetOutput);

            var err = _network.BackwardPropagate(targetOutput);

            return err;
        }

        void Validate(IVector output, IVector inputVector, IVector targetOutput)
        {
#if DEBUG
            if (output.ToColumnVector().Any(double.IsNaN))
            {
                var ex = new CalculationException();

                ex.Dump.VectorData[nameof(inputVector)] = inputVector;
                ex.Dump.VectorData[nameof(targetOutput)] = targetOutput;
                ex.Dump.VectorData[nameof(output)] = output;

                throw ex;
            }
#endif
        }
    }
}