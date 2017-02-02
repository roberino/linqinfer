using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinAppBuilder
    {
        Task ProcessContext(IOwinContext context);
        void AddComponent(Func<IOwinContext, Task> handler, OwinPipelineStage stage = OwinPipelineStage.PreHandlerExecute);
        void AddErrorHandler(Func<IOwinContext, Exception, Task<bool>> errorHandler);
    }
}