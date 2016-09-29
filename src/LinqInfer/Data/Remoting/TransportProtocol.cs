using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum TransportProtocol : byte
    {
        None = 0,
        Tcp = 1,
        Http = 2
    }
}