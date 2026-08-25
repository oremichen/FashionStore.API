using FashionStore.API.Features.ContactUs.CreateContact;
using FashionStore.API.Features.ContactUs.DeleteContact;
using FashionStore.API.Features.ContactUs.GetActiveContact;
using FashionStore.API.Features.ContactUs.GetAllContacts;
using FashionStore.API.Features.ContactUs.Shared;
using FashionStore.API.Features.ContactUs.UpdateContact;
using FashionStore.API.Features.ContactUs.SubmitContact;

namespace FashionStore.API.Features.ContactUs;

[Route("api/contact-us")]
[ApiController]
public sealed class ContactUsController(
    IGetAllContactsService getAllService,
    ICreateContactService createService,
    IUpdateContactService updateService,
    IDeleteContactService deleteService,
    IGetActiveContactService getActiveService,
    ISubmitContactService submitContactService) : BaseApiController
{
    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ContactUsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllContacts(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getAllService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateContact([FromBody] ContactUsRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateContact(string id, [FromBody] ContactUsRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteContact(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteService.ExecuteAsync(id, cancellationToken));
    }

    [AllowAnonymous, HttpGet("active")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ContactUsResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveContact(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getActiveService.ExecuteAsync(cancellationToken));
    }

    [AllowAnonymous, HttpPost("submit")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitContact([FromBody] SubmitContactRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await submitContactService.ExecuteAsync(request, cancellationToken));
    }
}
