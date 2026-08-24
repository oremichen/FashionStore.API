using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Notification
{
    public interface IEmailProvider
    {
        string Name { get; }

        Task<EmailProviderResult> SendAsync(
            EmailNotification notification,
            CancellationToken cancellationToken = default);
    }

    public sealed record EmailProviderResult(bool IsSuccessful, string? Error = null)
    {
        public static EmailProviderResult Success()
        {
            return new(true);
        }

        public static EmailProviderResult Failure(string error)
        {
            return new(false, error);
        }
    }
}
