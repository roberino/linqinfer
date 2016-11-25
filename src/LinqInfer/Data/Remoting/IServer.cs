using System;

namespace LinqInfer.Data.Remoting
{
    /// <summary>
    /// Represents a server which processes requests over a remote connection
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// Fires when the server status changes
        /// </summary>
        event EventHandler<EventArgsOf<ServerStatus>> StatusChanged;

        /// <summary>
        /// The server end-point (represented as a URI)
        /// </summary>
        Uri BaseEndpoint { get; }

        /// <summary>
        /// The server status
        /// </summary>
        ServerStatus Status { get; }

        /// <summary>
        /// Starts the server
        /// </summary>
        void Start();

        /// <summary>
        /// Sends the stop signal to the server
        /// </summary>
        /// <param name="wait">If true, the method will wait for server to stop completely</param>
        void Stop(bool wait = false);
    }
}