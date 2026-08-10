using System.Text.RegularExpressions;

namespace FashionStore.Domain.Entities;

public sealed class Category
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Category() { }

    private Category(string name, string slug, string? description, int sortOrder, bool isActive, bool showInMenu, string? parentId)
    {
        SetDetails(name, slug, description, sortOrder, isActive, showInMenu);
        AssignParent(parentId);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Id { get; private set; } = null!;
    public string? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public IReadOnlyCollection<Category> Children => _children;
    private readonly List<Category> _children = [];
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool ShowInMenu { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Category Create(string name, string slug, string? description, int sortOrder, bool isActive, bool showInMenu, string? parentId = null)
        => new(name, slug, description, sortOrder, isActive, showInMenu, parentId);

    public void AssignParent(string? parentId)
    {
        parentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId.Trim();
        if (parentId == Id)
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentId));

        ParentId = parentId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDetails(string name, string slug, string? description, int sortOrder, bool isActive, bool showInMenu)
    {
        name = name?.Trim() ?? string.Empty;
        slug = slug?.Trim() ?? string.Empty;

        if (name.Length is 0 or > 150)
            throw new ArgumentException("Category name must contain between 1 and 150 characters.", nameof(name));
        if (slug.Length is 0 or > 180 || !SlugPattern.IsMatch(slug))
            throw new ArgumentException("Slug must contain lowercase letters or numbers separated by single hyphens.", nameof(slug));
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");

        Name = name;
        Slug = slug;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        ShowInMenu = showInMenu;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
