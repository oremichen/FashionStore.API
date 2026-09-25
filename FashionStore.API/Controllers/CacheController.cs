using FashionStore.API.Caching;

namespace FashionStore.API.Controllers;

[ApiController]
[Route("api/admin/cache")]
[Authorize(Roles = RoleConstants.SuperAdmin)]
public sealed class CacheController(
    IRedisCacheService cache,
    ILogger<CacheController> logger) : BaseApiController
{
    [HttpDelete("response-cache")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<CachePurgeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PurgeResponseCache()
    {
        var deletedKeyCount = await cache.PurgeResponseCacheAsync();
        logger.LogInformation("Purged {DeletedKeyCount} Redis response-cache keys.", deletedKeyCount);

        var response = new ResponseResult<CachePurgeResponse>().Success(
            new CachePurgeResponse { DeletedKeyCount = deletedKeyCount },
            "Redis response cache purged successfully.");
        return ProcessResponse(response);
    }
}

public sealed class CachePurgeResponse
{
    public long DeletedKeyCount { get; init; }
}
