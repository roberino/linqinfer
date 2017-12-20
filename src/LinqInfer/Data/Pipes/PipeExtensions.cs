using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data.Pipes
{
    public static class PipeExtensions
    {
        public static IAsyncPipe<T> CreatePipe<T>(this IAsyncEnumerator<T> asyncEnumerator)
        {
            return new AsyncPipe<T>(asyncEnumerator);
        }

        public static IEnumerable<S> GetSinks<T, S>(this IAsyncPipe<T> pipe) where S : IAsyncSink<T>
        {
            return pipe.Sinks.Where(s => s is S).Cast<S>();
        }

        public static S GetSink<T, S>(this IAsyncPipe<T> pipe) where S : IAsyncSink<T>
        {
            return (S)pipe.Sinks.Where(s => s is S).FirstOrDefault();
        }
    }
}
