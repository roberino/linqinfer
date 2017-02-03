using LinqInfer.Data.Remoting;
using Owin;

namespace LinqInfer.Owin
{
    public interface IOwinApiBuilder : IHttpApiBuilder
    {
        IAppBuilder RegisterMiddleware(IAppBuilder appBuilder);
    }
}