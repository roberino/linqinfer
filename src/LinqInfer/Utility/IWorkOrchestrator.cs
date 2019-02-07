using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Utility
{
    interface IWorkOrchestrator : IDisposable
    {
        Task<T> EnqueueWork<T>(Func<T> work, CancellationToken cancellationToken = default);
    }
}