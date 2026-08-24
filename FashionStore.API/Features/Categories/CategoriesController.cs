using FashionStore.API.Features.Categories.CreateCategory;
using FashionStore.API.Features.Categories.GetCategories;
using FashionStore.API.Features.Categories.GetCategoriesWithParent;
using FashionStore.API.Features.Categories.GetCategoryById;
using FashionStore.API.Features.Categories.UpdateCategory;

namespace FashionStore.API.Features.Categories;

[Route("api/categories")]
[ApiController]
public sealed class CategoriesController(
    IGetCategoriesService getCategoriesService,
    IGetCategoryByIdService getCategoryByIdService,
    IGetCategoriesWithParentService getCategoriesWithParentService,
    ICreateCategoryService createCategoryService,
    IUpdateCategoryService updateCategoryService,
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
        return ProcessResponse(await getCategoriesService.ExecuteAsync(cancellationToken));
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
        return ProcessResponse(await getCategoryByIdService.ExecuteAsync(id, cancellationToken));
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
        return ProcessResponse(await getCategoriesWithParentService.ExecuteAsync(cancellationToken));
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
        return ProcessResponse(await createCategoryService.ExecuteAsync(request, cancellationToken));
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
        return ProcessResponse(await updateCategoryService.ExecuteAsync(id, request, cancellationToken));
    }
}
