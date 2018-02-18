using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public static class To
    {
        public static async Task<InMemorySink<T>> ToMemoryAsync<T>(this IAsyncSource<T> asyncEnumerator, CancellationToken cancellationToken, int? maxCapacity = null)
        {
            var asyncPipe = new AsyncPipe<T>(asyncEnumerator);
            var sink = new InMemorySink<T>(maxCapacity);

            asyncPipe.RegisterSinks(sink);

            await asyncPipe.RunAsync(cancellationToken);

            return sink;
        }
    }
}