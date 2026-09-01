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
            var normalizedUserId = Rules.Required(userId, 50, nameof(userId));
            var normalizedStreet = Rules.Required(street, 250, nameof(street));
            var normalizedCity = Rules.Required(city, 100, nameof(city));
            var normalizedState = Rules.Required(state, 100, nameof(state));
            var normalizedCountry = Rules.Required(country, 100, nameof(country));
            var normalizedPhoneNumber = Rules.RequiredPhone(phoneNumber, 50, nameof(phoneNumber));
            DemoteExistingMainAddress(isMain, existingAddresses);

            return new Address
            {
                UserId = normalizedUserId,
                Street = normalizedStreet,
                City = normalizedCity,
                State = normalizedState,
                Country = normalizedCountry,
                PostalCode = Rules.Optional(postalCode, 20, nameof(postalCode)),
                PhoneNumber = normalizedPhoneNumber,
                Landmark = Rules.Optional(landmark, 250, nameof(landmark)),
                IsMain = isMain
            };
        }

        public void Update(string street, string city, string state, string country,
            string? postalCode, string phoneNumber, string? landmark, bool isMain,
            IEnumerable<Address> otherAddresses)
        {
            var normalizedStreet = Rules.Required(street, 250, nameof(street));
            var normalizedCity = Rules.Required(city, 100, nameof(city));
            var normalizedState = Rules.Required(state, 100, nameof(state));
            var normalizedCountry = Rules.Required(country, 100, nameof(country));
            var normalizedPhoneNumber = Rules.RequiredPhone(phoneNumber, 50, nameof(phoneNumber));
            DemoteExistingMainAddress(isMain, otherAddresses);
            Street = normalizedStreet;
            City = normalizedCity;
            State = normalizedState;
            Country = normalizedCountry;
            PostalCode = Rules.Optional(postalCode, 20, nameof(postalCode));
            PhoneNumber = normalizedPhoneNumber;
            Landmark = Rules.Optional(landmark, 250, nameof(landmark));
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

    }
}
