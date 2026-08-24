using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Repositories.CatalogOptions;

public interface ICatalogOptionRepository
{
    Task<IReadOnlyList<Size>> GetSizesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Color>> GetColorsAsync(CancellationToken cancellationToken);
    Task<bool> SizeNameExistsAsync(string name, CancellationToken cancellationToken);
    Task<bool> ColorNameExistsAsync(string name, CancellationToken cancellationToken);
    Task<Size?> GetSizeByIdAsync(string id, CancellationToken cancellationToken);
    Task<Color?> GetColorByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> SizeHasProductsAsync(string id, CancellationToken cancellationToken);
    Task<bool> ColorHasProductsAsync(string id, CancellationToken cancellationToken);
    Task AddSizeAsync(Size size, CancellationToken cancellationToken);
    Task AddColorAsync(Color color, CancellationToken cancellationToken);
    Task DeleteSizeAsync(Size size, CancellationToken cancellationToken);
    Task DeleteColorAsync(Color color, CancellationToken cancellationToken);
}
