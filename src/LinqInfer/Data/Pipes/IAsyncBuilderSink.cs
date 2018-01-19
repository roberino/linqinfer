using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncBuilderSink<T, O> : IAsyncSink<T>
    {
        Task<O> BuildAsync();
    }
}