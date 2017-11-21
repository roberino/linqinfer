using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IMessagePublisher<T>
    {
        Task PublishAsync(Message<T> message);
    }
}