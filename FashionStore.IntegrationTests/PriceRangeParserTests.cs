using FashionStore.Domain.Abstractions.Products;
using Xunit;

namespace FashionStore.IntegrationTests;

public sealed class PriceRangeParserTests
{
    [Fact]
    public void Parses_multiple_and_open_ended_ranges()
    {
        var valid = PriceRangeParser.TryParse("0-10000,30000-50000,100000-", out var ranges);

        Assert.True(valid);
        Assert.Equal(
            [new PriceRange(0, 10000), new PriceRange(30000, 50000), new PriceRange(100000, null)],
            ranges);
    }

    [Theory]
    [InlineData("10000")]
    [InlineData("-")]
    [InlineData("20000-10000")]
    [InlineData("-1-10000")]
    [InlineData("one-10000")]
    public void Rejects_malformed_ranges(string value)
    {
        Assert.False(PriceRangeParser.TryParse(value, out _));
    }

    [Fact]
    public void Empty_value_means_no_price_range_filter()
    {
        Assert.True(PriceRangeParser.TryParse(null, out var ranges));
        Assert.Empty(ranges);
    }
}
