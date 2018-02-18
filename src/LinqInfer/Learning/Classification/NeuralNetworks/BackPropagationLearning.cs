using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class BackPropagationLearning : IAssistedLearningProcessor
    {
        private readonly MultilayerNetwork _network;
        private double _learningRate;
        protected readonly double _momentum;

        public BackPropagationLearning(MultilayerNetwork network, double momentum = 0.05)
        {
            network.Specification.Validate();

            Contract.Assert(momentum >= 0 && momentum <= 1);

            _network = network;
            _learningRate = network.Specification.LearningParameters.LearningRate;
            _momentum = momentum;
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _learningRate = rateAdjustment(_learningRate);
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
            var output = _network.Evaluate(inputVector);

            var errors = CalculateError(output, targetOutput);

            Adjust(errors.Item1);

            return errors.Item2 / 2; // Math.Sqrt(errors.Item2);
        }

        protected virtual Tuple<ColumnVector1D[], double> CalculateError(IVector actualOutput, IVector targetOutput)
        {
            // network
            //    -- layers[]
            //          -- neuron[]
            //              -- weights[]

            ILayer lastLayer = null;
            ColumnVector1D lastError = null;
            double error = 0;

            var errors = _network.ForEachLayer((layer) =>
            {
                if (lastError == null)
                {
                    lastError = layer.ForEachNeuron((n, k) =>
                    {
                        var e = targetOutput[k] - n.Output;
                        error += e * e;
                        return e * layer.Activator.Derivative(n.Output);
                    });
                }
                else
                {
                    lastError = layer.ForEachNeuron((n, i) =>
                    {
                        var err = lastLayer.ForEachNeuron((nk, k) =>
                        {
                            return lastError[k] * nk[i];
                        });

                        return err.Sum * layer.Activator.Derivative(n.Output);
                    });
                }

                lastLayer = layer;

                return lastError;
            }).Reverse().ToArray();

            return new Tuple<ColumnVector1D[], double>(errors, error);
        }

        protected virtual void Adjust(ColumnVector1D[] errors)
        {
            ILayer previousLayer = null;
            var i = 0;

            _network.ForEachLayer(layer =>
            {
                var layerErrors = errors[i++];

                var update = layer.ForEachNeuron((n, j) =>
                {
                    var error = layerErrors[j];

                    n.Adjust((w, k) =>
                    {
                        var prevOutput = previousLayer == null || k < 0 ? 1 : previousLayer[k].Output;
                        return ExecuteUpdateRule(w, error, prevOutput);
                    });

                    return 0;
                });

                previousLayer = layer;

                return update;
            }, false);
        }

        protected virtual double ExecuteUpdateRule(double currentWeightValue, double error, double previousLayerOutput)
        {
            return currentWeightValue + (_learningRate * ((_momentum * currentWeightValue) + ((1.0 - _momentum) * (error * previousLayerOutput))));
        }
    }
}