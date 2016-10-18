using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinContext : IDictionary<string, object>
    {
        ClaimsPrincipal User { get; }
        Uri RequestUri { get; }
        TcpRequest Request { get; }
        TcpResponse Response { get; }
        void Cancel();
        Task WriteTo(Stream output);
    }
}