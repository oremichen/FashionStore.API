using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionStore.API.Controllers
{
    [Authorize(Roles = "SuperAdmin, BusinessAdmin")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {

    }
}
