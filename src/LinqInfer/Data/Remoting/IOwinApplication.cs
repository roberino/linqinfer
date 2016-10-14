using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinApplication : IServer
    {
        void AddComponent(Func<IOwinContext, Task> handler, OwinPipelineStage stage = OwinPipelineStage.PreHandlerExecute);
    }
}