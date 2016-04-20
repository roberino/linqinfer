using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    internal class BackPropagationLearning : IAssistedLearningProcessor
    {
        private readonly MultilayerNetwork _network;
        private readonly double _learningRate;

        public BackPropagationLearning(MultilayerNetwork network, double learningRate = 1)
        {
            _network = network;
            _learningRate = learningRate;
        }
        public double Train(IEnumerable<Tuple<ColumnVector1D, ColumnVector1D>> trainingSet, double errorThreshold = 0, Func<ColumnVector1D, ColumnVector1D> preprocessor = null)
        {
            var pp = preprocessor ?? Functions.CreateNormalisingFunction(trainingSet.Select(x => x.Item1));

            double errTotal = 0;
            double err = 0;

            foreach(var inputPair in trainingSet)
            {
                errTotal += (err = Train(pp(inputPair.Item1), inputPair.Item2));

                if (err < errorThreshold) break;
            }

            return errTotal;
        }

        public double Train(ColumnVector1D inputVector, ColumnVector1D targetOutput)
        {
            var output = _network.Evaluate(inputVector);

            var errors = CalculateError(output, targetOutput);

            Adjust(errors.Item1);

            return errors.Item2;
        }

        protected virtual Tuple<ColumnVector1D[], double> CalculateError(ColumnVector1D actualOutput, ColumnVector1D targetOutput)
        {
            // network
            //    -- layers[]
            //          -- neuron[]
            //              -- weights[]

            ILayer lastLayer = null;
            ColumnVector1D lastError = null;
            double error = 0;

            var j = 0;

            var errors = _network.ForEachLayer((layer) =>
            {
                if (lastError == null)
                {
                    lastError = layer.ForEachNeuron((n, k) =>
                    {
                        var e = targetOutput[k] - n.Output;
                        error += e * e;
                        return e * _network.Activator.Derivative(n.Output);
                    });
                }
                else
                {
                    lastError = layer.ForEachNeuron((n, k) =>
                    {
                        var err = lastLayer.ForEachNeuron((n0, i) =>
                        {
                            return n0.Calculate(w => w * lastError[i]).Sum();
                        });

                        return err.Sum() * _network.Activator.Derivative(n.Output);
                    });
                }

                lastLayer = layer;

                j++;

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
                var lastBiasUpdate = 0d;
                var layerErrors = errors[i++];

                var update = layer.ForEachNeuron((n, j) =>
                {
                    var error = layerErrors[j];
                    var prevOutput = previousLayer == null ? 1 : previousLayer[j].Output;

                    var x = 0d;
                    n.Adjust(w => {
                        x = ExecuteUpdateRule(x, error, prevOutput);
                        return w + x;
                    });

                    lastBiasUpdate = _learningRate * error;

                    n.Bias += lastBiasUpdate;

                    return lastBiasUpdate;
                });

                previousLayer = layer;

                return update;
            }, false).ToArray();
        }

        protected virtual double ExecuteUpdateRule(double currentWeightValue, double error, double previousLayerOutput)
        {
            return _learningRate * (error * previousLayerOutput);
        }
    }
}