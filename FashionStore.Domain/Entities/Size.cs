namespace FashionStore.Domain.Entities;
public sealed class Size
{
    private Size() { }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static Size Create(string name, string displayName, int sortOrder = 0)
    {
        CatalogRules.NonNegative(sortOrder, nameof(sortOrder));
        return new Size { Name = CatalogRules.Required(name, 50, nameof(name)), DisplayName = CatalogRules.Required(displayName, 100, nameof(displayName)), SortOrder = sortOrder };
    }
}
