using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface INetworkSignalFilter : IExportableAsDataDocument, IPropagatedOutput
    {
        /// <summary>
        /// The size of the output vector
        /// </summary>
        // int OutputVectorSize { get; }

        /// <summary>
        /// Enqueues input to be processed
        /// </summary>
        void Receive(IVector input);
    }
}