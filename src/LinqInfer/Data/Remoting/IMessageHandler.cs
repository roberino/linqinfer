using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IMessageHandler
    {
        Task HandleAsync(Message message);
    }
}