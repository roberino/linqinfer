using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace LinqInfer.Genetics
{
    /// <summary>
    /// Simple genetic optimiser for tuning parameters of an algorithm. The <see cref="AlgorithmOptimiser"/>
    /// attempts to find the best set of parameters with regards to achieving the maximum
    /// "fitness" of a function.
    /// </summary>
    public sealed class AlgorithmOptimiser
    {
        public AlgorithmOptimiser()
        {
            Parameters = new AlgorithmParameterSet();
        }

        /// <summary>
        /// Resets parameters back to their initial state
        /// </summary>
        public void Reset()
        {
            Parameters.Reset();
        }

        /// <summary>
        /// Returns parameters which have been defined for the algorithm
        /// </summary>
        public AlgorithmParameterSet Parameters { get; }

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
                var score = algorithm(Parameters);

                if (!Parameters.SetOutcome(score, n))
                {
                    break;
                }

                // if (iterations == n) DebugOutput.Log(score);
            }

            return Parameters.OptimalParameters;
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
            public T Define<T>(string key, T parameter) where T : MutatableParameter
            {
                Contract.Assert(parameter != null);

                _params[key] = parameter;

                return parameter;
            }

            /// <summary>
            /// Defines a double presision floating point parameter
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="categories">The set of categories to choose from</param>
            /// <param name="initialValue">The initial value</param>
            /// <returns>A <see cref="MutatableCategoricalParameter{T}"/></returns>
            public MutatableCategoricalParameter<T> DefineCategoricalVariable<T>(string key, T initialValue, params T[] categories)
            {
                return Define(key, new MutatableCategoricalParameter<T>(initialValue, new HashSet<T>(categories)));
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
                return Define(key, new MutatableDoubleParameter(initialValue, minValue, maxValue));
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
                return Define(key, new MutatableIntegerParameter(initialValue, minValue, maxValue));
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

            /// <summary>
            /// Gets a parameter by key
            /// </summary>
            public T GetParameter<T>(string key) where T : MutatableParameter
            {
                return _params[key] as T;
            }

            /// <summary>
            /// Writes out the current values to the text writer
            /// </summary>
            public void PrintCurrentValues(TextWriter writer)
            {
                foreach (var p in _params)
                {
                    writer.WriteLine("{0} = {1}", p.Key, p.Value.CurrentValue);
                }
            }

            internal bool SetOutcome(double fitnessScore, int iteration)
            {
                if (!_params.Any()) return false;

                foreach (var parameter in _params.Where(p => p.Value.WasMutated))
                {
                    parameter.Value.Score(fitnessScore);
                }

                var unmutated = _params.Where(p => p.Value.WasAccessed && !p.Value.IsExhausted && (p.Value.MutationCounter < 5 || !p.Value.ValueFitnessScoreCovariance.HasValue)).RandomOrder().FirstOrDefault().Value;

                if (unmutated != null)
                {
                    unmutated.Mutate();
                }
                else
                {
                    if (iteration % (_params.Count * 5) == 0)
                    {
                        foreach(var p in _params)
                        {
                            p.Value.Optimise();
                        }

                        return true;
                    } 

                    var bestCandidateForMutation = _params.Where(p => p.Value.WasAccessed).OrderByDescending(p => Math.Abs(p.Value.ValueFitnessScoreCovariance.Value)).FirstOrDefault().Value;

                    if (bestCandidateForMutation != null) bestCandidateForMutation.Mutate();
                }

                return !_params.All(p => p.Value.IsExhausted);
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