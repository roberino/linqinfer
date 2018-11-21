using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface INetworkSignalFilter : IExportableAsDataDocument, IPropagatedOutput
    {
        string Id {get;}

        /// <summary>
        /// Returns all input sources (reoccurring and predecessors)
        /// </summary>
        IEnumerable<INetworkSignalFilter> Inputs { get; }

        /// <summary>
        /// Executes an action through the network, pushing it forward to successive modules
        /// </summary>
        void ForwardPropagate(Action<INetworkSignalFilter> work);
        
        /// <summary>
        /// Executes an action through the network, pushing it backward to previous modules
        /// </summary>
        double BackwardPropagate(IVector targetOutput, Vector previousError = null);

        /// <summary>
        /// Enqueues input to be processed
        /// </summary>
        void Receive(IVector input);
    }
}