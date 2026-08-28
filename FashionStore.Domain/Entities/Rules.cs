using System.Text.RegularExpressions;
using System.Net.Mail;

namespace FashionStore.Domain.Entities;

internal static class Rules
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

    internal static string RequiredEmail(string? value, int maximumLength, string name)
    {
        var email = Required(value, maximumLength, name).ToLowerInvariant();
        ValidateEmail(email, name);
        return email;
    }

    internal static string? OptionalEmail(string? value, int maximumLength, string name)
    {
        var email = Optional(value, maximumLength, name)?.ToLowerInvariant();
        if (email is null)
        {
            return null;
        }

        ValidateEmail(email, name);
        return email;
    }

    internal static string RequiredPhone(string? value, int maximumLength, string name)
    {
        var phone = Required(value, maximumLength, name);
        ValidatePhone(phone, name);
        return phone;
    }

    internal static string? OptionalPhone(string? value, int maximumLength, string name)
    {
        var phone = Optional(value, maximumLength, name);
        if (phone is null)
        {
            return null;
        }

        ValidatePhone(phone, name);
        return phone;
    }

    internal static void NonNegative(decimal value, string name)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }

    internal static void NonNegative(int value, string name)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }

    private static void ValidateEmail(string email, string name)
    {
        if (!MailAddress.TryCreate(email, out var mailAddress) ||
            !string.Equals(mailAddress.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{name} must be a valid email address.", name);
        }
    }

    private static void ValidatePhone(string phone, string name)
    {
        if (!Regex.IsMatch(phone, @"^\+?[0-9][0-9\s().-]*[0-9]$"))
        {
            throw new ArgumentException($"{name} must be a valid phone number.", name);
        }

        var digitCount = 0;
        foreach (var character in phone)
        {
            if (char.IsDigit(character))
            {
                digitCount++;
            }
        }

        if (digitCount < 7 || digitCount > 15)
        {
            throw new ArgumentException($"{name} must contain between 7 and 15 digits.", name);
        }
    }
}
