using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinContext : IDictionary<string, object>
    {
        Stream RequestBody { get; }
        TcpRequestHeader RequestHeader { get; }
        Uri RequestUri { get; }
        TcpResponse Response { get; }
    }
}