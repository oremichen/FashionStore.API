using System;
using System.Threading.Channels;
using FashionStore.Infrastructure.Contracts.Abstractions.Notification;
namespace FashionStore.Infrastructure.Messages.NotificationQueue
{
    public class EmailNotificationQueueService : IEmailNotificationQueueService, IAsyncDisposable
    {
        private readonly Channel<(Guid Id, EmailNotification Notification)> _channel;
        private readonly ILogger<EmailNotificationQueueService> _logger;

        public EmailNotificationQueueService(ILogger<EmailNotificationQueueService> logger)
        {
            _logger = logger;
            // BoundedCapacity prevents unbounded memory growth
            _channel = Channel.CreateBounded<(Guid, EmailNotification)>(new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        public void Enqueue(Guid notificationId, EmailNotification notification)
        {
            if (!_channel.Writer.TryWrite((notificationId, notification)))
            {
                _logger.LogWarning(
                    "Notification {NotificationId} was persisted but could not be added to the in-memory queue. It will be recovered by the retry processor.",
                    notificationId);
            }
        }

        public ChannelReader<(Guid Id, EmailNotification Notification)> Reader
        {
            get
            {
                _logger.LogDebug("Accessing the email notification queue reader.");
                return _channel.Reader;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();
            await _channel.Reader.Completion;
        }
    }

}
