using System.Globalization;

namespace FashionStore.Domain.Abstractions.Products;

public readonly record struct PriceRange(decimal? Minimum, decimal? Maximum);

public static class PriceRangeParser
{
    private const int MaximumRangeCount = 20;

    public static bool TryParse(string? value, out IReadOnlyList<PriceRange> ranges)
    {
        ranges = [];
        if (string.IsNullOrWhiteSpace(value)) return true;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Length > MaximumRangeCount) return false;

        var parsed = new List<PriceRange>(parts.Length);
        foreach (var part in parts)
        {
            var separator = part.IndexOf('-');
            if (separator < 0 || separator != part.LastIndexOf('-')) return false;

            var minimumText = part[..separator].Trim();
            var maximumText = part[(separator + 1)..].Trim();
            if (minimumText.Length == 0 && maximumText.Length == 0) return false;
            if (!TryParseBound(minimumText, out var minimum) || !TryParseBound(maximumText, out var maximum)) return false;
            if (minimum < 0 || maximum < 0 || (minimum.HasValue && maximum.HasValue && minimum >= maximum)) return false;

            var range = new PriceRange(minimum, maximum);
            if (!parsed.Contains(range)) parsed.Add(range);
        }

        ranges = parsed;
        return true;
    }

    private static bool TryParseBound(string value, out decimal? bound)
    {
        bound = null;
        if (value.Length == 0) return true;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) return false;
        bound = parsed;
        return true;
    }
}
