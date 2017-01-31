using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Remoting
{
    public interface IHttpHeader
    {
        IDictionary<string, string[]> Headers { get; }
        string HttpProtocol { get; }
    }
}