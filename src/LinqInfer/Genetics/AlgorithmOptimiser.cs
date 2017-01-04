using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Genetics
{
    public sealed class AlgorithmOptimiser
    {
        private readonly AlgorithmParameterSet _parameters;

        public AlgorithmOptimiser()
        {
            _parameters = new AlgorithmParameterSet();
        }

        /// <summary>
        /// Resets parameters back to their initial state
        /// </summary>
        public void Reset()
        {
            _parameters.Reset();
        }

        /// <summary>
        /// Returns parameters which have been defined for the algorithm
        /// </summary>
        public AlgorithmParameterSet Parameters { get { return _parameters; } }

        /// <summary>
        /// Invokes a function over a number of iterations seeking optimal parameters
        /// </summary>
        /// <param name="algorithm">The function</param>
        /// <param name="iterations">The number of iterations</param>
        /// <returns></returns>
        public IDictionary<string, object> Optimise(Func<AlgorithmParameterSet, double> algorithm, int iterations = 100)
        {
            foreach (var n in Enumerable.Range(1, iterations))
            {
                var score = algorithm(_parameters);

                _parameters.SetOutcome(score);

                // if (iterations == n) DebugOutput.Log(score);
            }

            return _parameters.OptimalParameters;
        }

        public sealed class AlgorithmParameterSet
        {
            private readonly IDictionary<string, MutatableParameter> _params;

            internal AlgorithmParameterSet()
            {
                _params = new Dictionary<string, MutatableParameter>();
            }

            /// <summary>
            /// Defines a parameter which can be accessed and optimised
            /// </summary>
            /// <param name="key">The parameter name (key)</param>
            /// <param name="parameter">The parameter definition</param>
            public MutatableParameter Define(string key, MutatableParameter parameter)
            {
                Contract.Assert(parameter != null);

                _params[key] = parameter;

                return parameter;
            }

            /// <summary>
            /// Defines a double presision floating point parameter
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="minValue">The minimum value</param>
            /// <param name="maxValue">The maximum value</param>
            /// <param name="initialValue">The initial value</param>
            /// <returns>A <see cref="MutatableDoubleParameter"/></returns>
            public MutatableDoubleParameter DefineDouble(string key, double minValue, double maxValue, double initialValue = 0)
            {
                var parameter = new MutatableDoubleParameter(initialValue, minValue, maxValue);

                _params[key] = parameter;

                return parameter;
            }

            /// <summary>
            /// Defines a integral parameter
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="minValue">The minimum value</param>
            /// <param name="maxValue">The maximum value</param>
            /// <param name="initialValue">The initial value</param>
            /// <returns>A <see cref="MutatableIntegerParameter"/></returns>
            public MutatableIntegerParameter DefineInteger(string key, int minValue, int maxValue, int initialValue = 0)
            {
                var parameter = new MutatableIntegerParameter(initialValue, minValue, maxValue);

                _params[key] = parameter;

                return parameter;
            }

            /// <summary>
            /// Returns the current optimised parameter set
            /// </summary>
            public IDictionary<string, object> OptimalParameters
            {
                get
                {
                    return _params.ToDictionary(p => p.Key, p => p.Value.OptimalValue);
                }
            }

            /// <summary>
            /// Gets the current optimised value
            /// </summary>
            /// <typeparam name="T">The type of parameter</typeparam>
            /// <param name="key">The parameter key</param>
            /// <returns>The type safe value</returns>
            public T GetValue<T>(string key)
            {
                return (T)_params[key].CurrentValue;
            }

            public MutatableParameter GetParameter(string key)
            {
                return _params[key];
            }

            internal void SetOutcome(double fitnessScore)
            {
                if (!_params.Any()) return;

                foreach (var parameter in _params.Where(p => p.Value.WasMutated))
                {
                    parameter.Value.Score(fitnessScore);
                }

                var unmutated = _params.Where(p => p.Value.MutationCounter < 5 || !p.Value.ValueFitnessScoreCovariance.HasValue).RandomOrder().FirstOrDefault().Value;

                if (unmutated != null)
                {
                    unmutated.Mutate();
                }
                else
                {
                    var bestCandidateForMutation = _params.OrderByDescending(p => Math.Abs(p.Value.ValueFitnessScoreCovariance.Value)).First();

                    bestCandidateForMutation.Value.Mutate();
                }
            }

            internal void Reset()
            {
                foreach (var parameter in _params)
                {
                    parameter.Value.Reset();
                }
            }
        }
    }
}