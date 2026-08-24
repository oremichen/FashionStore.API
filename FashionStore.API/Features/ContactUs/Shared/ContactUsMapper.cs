namespace FashionStore.API.Features.ContactUs.Shared;

internal static class ContactUsMapper
{
    internal static ContactUsResponse Map(FashionStore.Domain.Entities.ContactUs contact)
    {
        return new ContactUsResponse
        {
            Id = contact.Id,
            Address = contact.Address,
            ContactPhone = contact.ContactPhone,
            BusinessPhone = contact.BusinessPhone,
            ContactEmail = contact.ContactEmail,
            BusinessEmail = contact.BusinessEmail,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            IsActive = contact.IsActive
        };
    }
}
