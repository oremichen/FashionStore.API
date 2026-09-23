namespace FashionStore.Domain.Entities;

public sealed class Type
{
    private Type()
    {
    }

    private Type(string name, string slug, string? description, bool isActive)
    {
        ApplyDetails(name, slug, description, isActive);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Type Create(string name, string slug, string? description, bool isActive = true)
    {
        return new Type(name, slug, description, isActive);
    }

    public void SetDetails(string name, string slug, string? description, bool isActive)
    {
        ApplyDetails(name, slug, description, isActive);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ApplyDetails(string name, string slug, string? description, bool isActive)
    {
        Name = Rules.Required(name, 150, nameof(name));
        Slug = Rules.Slug(slug, 150);
        Description = Rules.Optional(description, 500, nameof(description));
        IsActive = isActive;
    }
}
