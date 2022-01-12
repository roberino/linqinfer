using System.Collections.Generic;

namespace LinqInfer.Maths.Probability
{
    /// <summary>
    /// Simulates the transition from one state to another
    /// </summary>
    public interface ITransitionSimulator<T>
    {
        /// <summary>
        /// Simulates a sequence of state transitions
        /// over a maximum number of iterations
        /// </summary>
        /// <param name="maxIterations">The maximum number of iterations</param>
        /// <returns>An enumeration of states</returns>
        IEnumerable<T> Simulate(int maxIterations = 100);
        
        /// <summary>
        /// Simulates a sequence of state transitions based on a seed value
        /// over a maximum number of iterations
        /// </summary>
        /// <param name="seedState">An initial state</param>
        /// <param name="maxIterations">The maximum number of iterations</param>
        /// <returns>An enumeration of states</returns>
        IEnumerable<T> Simulate(T seedState, int maxIterations = 100);

        /// <summary>
        /// Simulates the next state given a current state
        /// </summary>
        /// <param name="currentState">The current state</param>
        /// <returns>The next state</returns>
        T SimulateNext(T currentState);

        /// <summary>
        /// Simulates the next state given a current sequence of states
        /// </summary>
        /// <param name="transitionStates">The prior states</param>
        /// <returns>The next state</returns>
        T SimulateNext(IEnumerable<T> transitionStates);
    }
}