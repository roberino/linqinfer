﻿using LinqInfer.Learning.Features;
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

        public BackPropagationLearning(MultilayerNetwork network)
        {
            network.Specification.Validate();

            _network = network;
            _learningRate = network.Specification.LearningParameters.LearningRate;
            _momentum = network.Specification.LearningParameters.Momentum;
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

            Validate(output, inputVector, targetOutput);

            var errors = CalculateError(output, targetOutput);

            Adjust(errors.Item1);

            return errors.Item2;
        }

        protected virtual Tuple<Vector[], double> CalculateError(IVector actualOutput, IVector targetOutput)
        {
            // network
            //    -- layers[]
            //          -- neuron[]
            //              -- weights[]

            ILayer lastLayer = null;
            Vector lastError = null;
            double error = 0;

            var errors = _network.ForEachLayer((layer) =>
            {
                if (lastError == null)
                {
                    var errAndLoss = layer.LossFunction.Calculate(layer.LastOutput, targetOutput, layer.Activator.Derivative);
                    
                    error += errAndLoss.Loss;

                    lastError = errAndLoss.DerivativeError;
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

            return new Tuple<Vector[], double>(errors, error);
        }

        protected virtual void Adjust(Vector[] errors)
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

        private void Validate(IVector output, IVector inputVector, IVector targetOutput)
        {
#if DEBUG
            if (output.ToColumnVector().Any(x => x == double.NaN))
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