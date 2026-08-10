using System.Text.RegularExpressions;

namespace FashionStore.Domain.Entities;

internal static class CatalogRules
{
    internal static string Required(string? value, int maximumLength, string name)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > maximumLength)
            throw new ArgumentException($"{name} must contain between 1 and {maximumLength} characters.", name);
        return result;
    }

    internal static string? Optional(string? value, int maximumLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        if (result.Length > maximumLength)
            throw new ArgumentException($"{name} cannot exceed {maximumLength} characters.", name);
        return result;
    }

    internal static string Slug(string? value, int maximumLength)
    {
        var slug = Required(value, maximumLength, "slug");
        if (!Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Slug must contain lowercase letters or numbers separated by single hyphens.", nameof(value));
        return slug;
    }

    internal static void NonNegative(decimal value, string name)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }

    internal static void NonNegative(int value, string name)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }
}
