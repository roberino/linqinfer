using LinqInfer.Data.Pipes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    class MessageSink<T> : IAsyncSink<T>
    {
        readonly IMessagePublisher _messagePublisher;
        readonly Func<IBatch<T>, Message> _messageConverter;

        public MessageSink(IMessagePublisher messagePublisher, Func<IBatch<T>, Message> messageConverter)
        {
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _messageConverter = messageConverter ?? throw new ArgumentNullException(nameof(messageConverter));
        }

        public bool CanReceive => true;

        public async Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
        {
            var msg = _messageConverter(dataBatch);

            await _messagePublisher.PublishAsync(msg);
        }
    }
}