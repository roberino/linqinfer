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
            public Task<T> EnqueueWork<T>(Func<T> work, CancellationToken cancellationToken = default)
            {
                return Task.Run(work, cancellationToken);
            }

            public void Dispose()
            {
            }
        }

        class InlineWorkOrchestrator : IWorkOrchestrator
        {
            public Task<T> EnqueueWork<T>(Func<T> work, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(work());
            }

            public void Dispose()
            {
            }
        }
    }
}