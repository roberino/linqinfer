namespace LinqInfer.Data.Remoting
{
    public enum ServerStatus
    {
        /// <summary>
        /// The status is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// The server is connecting
        /// </summary>
        Connecting,

        /// <summary>
        /// The server is running and ready to respond to requests
        /// </summary>
        Running,

        /// <summary>
        /// The server is shutting down
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// The server has been stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// The server is in an error state and not responding to requests
        /// </summary>
        Error,

        /// <summary>
        /// The server connection has broken and the server may attempt to restore it
        /// </summary>
        Broken,

        /// <summary>
        /// The server is restoring a broken connection
        /// </summary>
        Restoring,

        /// <summary>
        /// The server has been disposed
        /// </summary>
        Disposed
    }
}