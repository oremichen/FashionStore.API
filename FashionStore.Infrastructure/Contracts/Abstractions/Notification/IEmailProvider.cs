namespace FashionStore.Infrastructure.Contracts.Abstractions.Notification
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
        public static EmailProviderResult Success() => new(true);

        public static EmailProviderResult Failure(string error) => new(false, error);
    }
}
