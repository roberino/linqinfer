using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IBuilder<T, O> : IAsyncSink<T>
    {
        Task<O> BuildAsync();
    }
}