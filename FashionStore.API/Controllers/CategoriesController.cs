using FashionStore.Application.Abstractions.Categories;

namespace FashionStore.API.Controllers;

[Route("api/categories")]
[ApiController]
public sealed class CategoriesController(
    ICategoryService categoryService,
    ILogger<CategoriesController> logger) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving public categories.");
        return ProcessResponse(await categoryService.GetCategoriesAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving category {CategoryId}.", id);
        return ProcessResponse(await categoryService.GetByIdAsync(id, cancellationToken));
    }

    // Per the requested definition, these are categories whose ParentId is populated.
    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("with-parent")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesWithParent(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving categories with parents.");
        return ProcessResponse(await categoryService.GetCategoriesWithParentAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category with slug {Slug}.", request.Slug);
        return ProcessResponse(await categoryService.CreateAsync(request, cancellationToken));
    }
}
