using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IMessagePublisher
    {
        Task PublishAsync(Message message);
    }
}