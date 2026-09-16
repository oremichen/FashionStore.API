namespace FashionStore.Domain.Abstractions.Notification;

public interface ISmsProvider
{
    Task<SmsProviderResult> SendAsync(
        SmsMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record SmsMessage(string To, string Body, string? From = null);

public sealed record SmsProviderResult(bool IsSuccessful, string? MessageSid = null, string? Error = null)
{
    public static SmsProviderResult Success(string? messageSid)
    {
        return new(true, messageSid);
    }

    public static SmsProviderResult Failure(string error)
    {
        return new(false, null, error);
    }
}
