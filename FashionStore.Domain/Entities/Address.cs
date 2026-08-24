namespace FashionStore.Domain.Entities
{
    public class Address
    {
        public string Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string? PostalCode { get; set; }
        public string PhoneNumber { get; set; }
        public string? Landmark { get; set; }
        public bool IsMain { get; set; } = false;

        // Foreign Key
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public Address() { }

        public static Address Create(
            string userId, 
            string street, 
            string city, 
            string state,
            string country, 
            string? postalCode,
            string phoneNumber,
            string? landmark,
            bool isMain, 
            IEnumerable<Address> existingAddresses)
        {
            Validate(userId, street, city, state, country, phoneNumber);
            DemoteExistingMainAddress(isMain, existingAddresses);

            return new Address
            {
                UserId = userId.Trim(),
                Street = street.Trim(),
                City = city.Trim(),
                State = state.Trim(),
                Country = country.Trim(),
                PostalCode = NormalizeOptional(postalCode),
                PhoneNumber = phoneNumber.Trim(),
                Landmark = NormalizeOptional(landmark),
                IsMain = isMain
            };
        }

        public void Update(string street, string city, string state, string country,
            string? postalCode, string phoneNumber, string? landmark, bool isMain,
            IEnumerable<Address> otherAddresses)
        {
            Validate(UserId, street, city, state, country, phoneNumber);
            DemoteExistingMainAddress(isMain, otherAddresses);
            Street = street.Trim();
            City = city.Trim();
            State = state.Trim();
            Country = country.Trim();
            PostalCode = NormalizeOptional(postalCode);
            PhoneNumber = phoneNumber.Trim();
            Landmark = NormalizeOptional(landmark);
            IsMain = isMain;
        }

        private static void DemoteExistingMainAddress(bool isMain, IEnumerable<Address> addresses)
        {
            if (!isMain)
                return;

            foreach (var address in addresses.Where(address => address.IsMain))
            {
                address.IsMain = false;
            }
        }

        private static void Validate(string userId, string street, string city, string state,
            string country, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id is required.");
            if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required.");
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.");
            if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("State is required.");
            if (string.IsNullOrWhiteSpace(country)) throw new ArgumentException("Country is required.");
            if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required.");
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
