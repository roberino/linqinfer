using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinContext : IDictionary<string, object>
    {
        ClaimsPrincipal User { get; }
        Stream RequestBody { get; }
        TcpRequestHeader RequestHeader { get; }
        Uri RequestUri { get; }
        TcpResponse Response { get; }
        void Cancel();
    }
}