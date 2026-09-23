namespace FashionStore.API.Features.Types.Shared;

using TypeEntity = FashionStore.Domain.Entities.Type;

public static class TypeResponseMapper
{
    public static TypeResponse Map(TypeEntity type)
    {
        return new TypeResponse
        {
            Id = type.Id,
            Name = type.Name,
            Slug = type.Slug,
            Description = type.Description,
            IsActive = type.IsActive,
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt
        };
    }
}
