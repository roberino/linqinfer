using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IMessageHandler<T>
    {
        Task HandleAsync(Message<T> message);
    }
}