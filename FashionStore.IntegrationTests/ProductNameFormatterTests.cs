using FashionStore.API.Features.Products.Shared;
using Xunit;

namespace FashionStore.IntegrationTests;

public sealed class ProductNameFormatterTests
{
    [Theory]
    [InlineData("cotton summer dress", "Cotton Summer Dress")]
    [InlineData("USB cotton t-shirt", "USB Cotton T-Shirt")]
    [InlineData("men's linen shirt", "Men's Linen Shirt")]
    [InlineData("  cotton  shirt\tblue", "  Cotton  Shirt\tBlue")]
    [InlineData("écru linen dress", "Écru Linen Dress")]
    [InlineData("", "")]
    public void Capitalizes_word_initials_and_preserves_remaining_characters(string input, string expected)
    {
        Assert.Equal(expected, ProductNameFormatter.CapitalizeWords(input));
    }
}