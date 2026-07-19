namespace FashionStore.Domain.Entities
{
    public class QueueEmailNotification
    {
        public Guid Id { get; set; }

        public string? From { get; set; }

        public string ToRecipients { get; set; } = string.Empty;

        public string? CcRecipients { get; set; }

        public string? BccRecipients { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string Status { get; set; } = "Processing";

        public int RetryCount { get; set; }

        public string? LastError { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset FailedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
