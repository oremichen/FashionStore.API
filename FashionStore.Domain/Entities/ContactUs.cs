namespace FashionStore.Domain.Entities;

public sealed class ContactUs
{
    private ContactUs() { }

    private ContactUs(string name, string email, string phone, string subject, string message)
    {
        Name = Rules.Required(name, 200, nameof(name));
        Email = Rules.RequiredEmail(email, 254, nameof(email));
        Phone = Rules.RequiredPhone(phone, 50, nameof(phone));
        Subject = Rules.Required(subject, 250, nameof(subject));
        Message = Rules.Required(message, 5000, nameof(message));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static ContactUs Create(string name, string email, string phone, string subject, string message) =>
        new(name, email, phone, subject, message);
}
