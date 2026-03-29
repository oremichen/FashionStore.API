using System;
using System.Threading.Channels;
using FashionStore.Application.Abstractions.Notification;
namespace FashionStore.Application.Messages.NotificationQueue
{
    public class EmailNotificationQueueService : IEmailNotificationQueueService, IAsyncDisposable
    {
        private readonly Channel<EmailNotification> _channel;

        public EmailNotificationQueueService()
        {
            // BoundedCapacity prevents unbounded memory growth
            _channel = Channel.CreateBounded<EmailNotification>(new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        public void Enqueue(EmailNotification metricsEvent)
        {
            // TryWrite is non-blocking - never slows down your controller
            _channel.Writer.TryWrite(metricsEvent);
        }

        public ChannelReader<EmailNotification> Reader => _channel.Reader;

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();
            await _channel.Reader.Completion;
        }
    }

}
