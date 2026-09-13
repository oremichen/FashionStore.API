namespace FashionStore.API.Features.ContactUs.Shared;

internal static class ContactUsMapper
{
    internal static ContactUsResponse Map(FashionStore.Domain.Entities.ContactUsConfiguration contact)
    {
        return new ContactUsResponse
        {
            Id = contact.Id,
            Address = contact.AddressDetails is null ? string.Empty : FormatAddress(contact.AddressDetails),
            Country = contact.AddressDetails?.Country ?? string.Empty,
            State = contact.AddressDetails?.State ?? string.Empty,
            City = contact.AddressDetails?.City ?? string.Empty,
            Street = contact.AddressDetails?.Street ?? string.Empty,
            ContactPhone = contact.ContactPhone,
            BusinessPhone = contact.BusinessPhone,
            ContactEmail = contact.ContactEmail,
            BusinessEmail = contact.BusinessEmail,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            IsActive = contact.IsActive
        };
    }

    private static string FormatAddress(FashionStore.Domain.Entities.Address address)
    {
        return string.Join(", ", new[] { address.Street, address.City, address.State, address.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
