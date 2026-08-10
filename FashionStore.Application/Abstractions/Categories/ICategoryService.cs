namespace FashionStore.Application.Abstractions.Categories;

public interface ICategoryService
{
    Task<ResponseResult<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<ResponseResult<CategoryDetailsResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<CategoryResponse>>> GetCategoriesWithParentAsync(CancellationToken cancellationToken);
    Task<ResponseResult<CategoryDetailsResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);
}
