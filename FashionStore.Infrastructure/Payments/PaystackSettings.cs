using System.ComponentModel.DataAnnotations;

namespace FashionStore.Infrastructure.Payments;

public sealed class PaystackSettings
{
    public const string SectionName = "Paystack";
    [Required] public string BaseUrl { get; set; } = "https://api.paystack.co";
    [Required] public string SecretKey { get; set; } = string.Empty;
}
