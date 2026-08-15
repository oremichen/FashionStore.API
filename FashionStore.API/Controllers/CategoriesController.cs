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
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving public categories.");
        return ProcessResponse(await categoryService.GetCategoriesAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving category {CategoryId}.", id);
        return ProcessResponse(await categoryService.GetByIdAsync(id, cancellationToken));
    }

    // Per the requested definition, these are categories whose ParentId is populated.
    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("with-parent")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoriesWithParent(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving categories with parents.");
        return ProcessResponse(await categoryService.GetCategoriesWithParentAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category with slug {Slug}.", request.Slug);
        return ProcessResponse(await categoryService.CreateAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult<CategoryDetailsResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating category {CategoryId}.", id);
        return ProcessResponse(await categoryService.UpdateAsync(id, request, cancellationToken));
    }
}
