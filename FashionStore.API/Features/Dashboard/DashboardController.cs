namespace FashionStore.API.Controllers
{
    [Authorize(Roles = "SuperAdmin, BusinessAdmin")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {

    }
}
