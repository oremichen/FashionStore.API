namespace FashionStore.Domain.Entities
{
    public class Address
    {
        public Guid Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }

        // Foreign Key
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
