using LinqInfer.Data.Remoting;
using Microsoft.AspNetCore.Builder;

namespace LinqInfer.AspNetCore
{
    public interface IOwinApiBuilder : IHttpApiBuilder
    {
        IApplicationBuilder RegisterMiddleware(IApplicationBuilder appBuilder);
    }
}