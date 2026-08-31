using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.CatalogOptions;

public interface ICatalogOptionRepository
{
    Task<(IReadOnlyList<Size> Items, int TotalCount)> GetSizesAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Color> Items, int TotalCount)> GetColorsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<bool> SizeNameExistsAsync(string name, CancellationToken cancellationToken);
    Task<bool> SizeNameExistsAsync(string name, string excludeId, CancellationToken cancellationToken);
    Task<bool> ColorNameExistsAsync(string name, CancellationToken cancellationToken);
    Task<Size?> GetSizeByIdAsync(string id, CancellationToken cancellationToken);
    Task<Color?> GetColorByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> SizeHasProductsAsync(string id, CancellationToken cancellationToken);
    Task<bool> ColorHasProductsAsync(string id, CancellationToken cancellationToken);
    Task AddSizeAsync(Size size, CancellationToken cancellationToken);
    Task AddColorAsync(Color color, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteSizeAsync(Size size, CancellationToken cancellationToken);
    Task DeleteColorAsync(Color color, CancellationToken cancellationToken);
}
