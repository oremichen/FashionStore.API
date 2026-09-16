namespace FashionStore.API.Controllers
{
    [Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {

    }
}
