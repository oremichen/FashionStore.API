namespace FashionStore.API.Features.Types.Shared;

using TypeEntity = FashionStore.Domain.Entities.Type;

public static class TypeResponseMapper
{
    public static TypeResponse Map(TypeEntity type, int productCount = 0)
    {
        return new TypeResponse
        {
            Id = type.Id,
            Name = type.Name,
            Slug = type.Slug,
            Description = type.Description,
            IsActive = type.IsActive,
            ProductCount = productCount,
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt
        };
    }
}
