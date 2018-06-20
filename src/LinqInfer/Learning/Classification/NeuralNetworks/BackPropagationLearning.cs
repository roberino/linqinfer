using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class BackPropagationLearning : IAssistedLearningProcessor
    {
        private readonly MultilayerNetwork _network;

        public BackPropagationLearning(MultilayerNetwork network)
        {
            network.Specification.Validate();

            _network = network;
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _network.ForEachLayer(l => l.WeightUpdateRule.AdjustLearningRate(rateAdjustment)).ToList();
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

            DebugOutput.LogVerbose("Error = {0}", errors.Item2);

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

                        var wp = new WeightUpdateParameters()
                        {
                            CurrentWeightValue = w,
                            Error = error,
                            PreviousLayerOutput = prevOutput
                        };

                        var wu = layer.WeightUpdateRule.Execute(wp);

                        // DebugOutput.Log($"w = {wu} => error = {wp.Error} previous output = {wp.PreviousLayerOutput}, w = {wp.CurrentWeightValue}");

                        return wu;
                    });

                    return 0;
                });

                previousLayer = layer;

                return update;
            }, false);
        }

        private void Validate(IVector output, IVector inputVector, IVector targetOutput)
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