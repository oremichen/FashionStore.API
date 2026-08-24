namespace FashionStore.API.Features.Users.Shared
{
    public class UserDetailsResponse
    {
        public string UserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = [];

        public List<UserAddressResponse> Addresses { get; set; } = [];
    }

    public sealed class UserAddressResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public bool IsMain { get; set; }

        public static UserAddressResponse From(Address address)
        {
            return new UserAddressResponse
            {
                Id = address.Id,
                Street = address.Street,
                City = address.City,
                State = address.State,
                Country = address.Country,
                PostalCode = address.PostalCode,
                PhoneNumber = address.PhoneNumber,
                Landmark = address.Landmark,
                IsMain = address.IsMain
            };
        }
    }
}
