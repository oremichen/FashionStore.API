using System.Text.RegularExpressions;

namespace FashionStore.API.Features.Products.Shared;

public static partial class ProductNameFormatter
{
    public static string CapitalizeWords(string name)
    {
        return WordStart().Replace(name, match =>
            match.Groups[1].Value + match.Groups[2].Value.ToUpperInvariant());
    }

    [GeneratedRegex(@"(^|[\s\-/([{])(\p{L})")]
    private static partial Regex WordStart();
}