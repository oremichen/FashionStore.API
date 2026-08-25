namespace FashionStore.Domain.Entities;

public sealed class ContactUsConfiguration
{
    private ContactUsConfiguration() { }

    private ContactUsConfiguration(
        string address,
        string contactPhone,
        string? businessPhone,
        string contactEmail,
        string? businessEmail,
        bool isActive)
    {
        SetDetails(address, contactPhone, businessPhone, contactEmail, businessEmail, isActive);
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string Address { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public string? BusinessPhone { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? BusinessEmail { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static ContactUsConfiguration Create(
        string address,
        string contactPhone,
        string? businessPhone,
        string contactEmail,
        string? businessEmail,
        bool isActive)
    {
        return new ContactUsConfiguration(address, contactPhone, businessPhone, contactEmail, businessEmail, isActive);
    }

    public void Update(
        string address,
        string contactPhone,
        string? businessPhone,
        string contactEmail,
        string? businessEmail,
        bool isActive)
    {
        SetDetails(address, contactPhone, businessPhone, contactEmail, businessEmail, isActive);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetDetails(
        string address,
        string contactPhone,
        string? businessPhone,
        string contactEmail,
        string? businessEmail,
        bool isActive)
    {
        Address = Rules.Required(address, 500, nameof(address));
        ContactPhone = Rules.RequiredPhone(contactPhone, 50, nameof(contactPhone));
        BusinessPhone = Rules.OptionalPhone(businessPhone, 50, nameof(businessPhone));
        ContactEmail = Rules.RequiredEmail(contactEmail, 254, nameof(contactEmail));
        BusinessEmail = Rules.OptionalEmail(businessEmail, 254, nameof(businessEmail));
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
