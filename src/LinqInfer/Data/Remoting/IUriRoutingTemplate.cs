using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Remoting
{
    public interface IUriRouteMapper
    {
        bool IsTarget(IOwinContext context);
        bool TryMap(Uri uri, Verb verb, out IDictionary<string, string> parameters);
    }
}