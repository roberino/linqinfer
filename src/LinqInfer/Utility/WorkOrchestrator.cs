using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Utility
{
    class WorkOrchestrator
    {
        WorkOrchestrator() { }

        public static readonly IWorkOrchestrator ThreadPool = new ThreadPoolWorkOrchestrator();

        public static readonly IWorkOrchestrator Default = new InlineWorkOrchestrator();

        class ThreadPoolWorkOrchestrator : IWorkOrchestrator
        {
            public async ValueTask<T> EnqueueWork<T>(Func<T> work, CancellationToken cancellationToken = default) 
                => await Task.Run(work, cancellationToken);

            public void Dispose()
            {
            }
        }

        class InlineWorkOrchestrator : IWorkOrchestrator
        {
            public ValueTask<T> EnqueueWork<T>(Func<T> work, CancellationToken cancellationToken = default) 
                => new ValueTask<T>(work());

            public void Dispose()
            {
            }
        }
    }
}