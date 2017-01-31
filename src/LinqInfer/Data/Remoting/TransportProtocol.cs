using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum TransportProtocol : byte
    {
        /// <summary>
        /// Unknown protocol
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Raw (internal) TCP protocol
        /// </summary>
        Tcp = 1,

        /// <summary>
        /// HTTP
        /// </summary>
        Http = 2
    }
}