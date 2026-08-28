using System.Text.RegularExpressions;
namespace FashionStore.Domain.Entities;
public sealed class Color
{
    private Color() { }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? HexCode { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static Color Create(string name, string? hexCode, int sortOrder = 0)
    {
        Rules.NonNegative(sortOrder, nameof(sortOrder));
        if (hexCode is not null && !Regex.IsMatch(hexCode, "^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")) throw new ArgumentException("Invalid hex color.");
        return new Color { Name = Rules.Required(name, 100, nameof(name)), HexCode = hexCode, SortOrder = sortOrder };
    }
}
