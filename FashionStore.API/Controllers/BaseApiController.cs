using FashionStore.Shared.Constants;

namespace FashionStore.API.Controllers
{
    public class BaseApiController : ControllerBase
    {
        protected IActionResult ProcessResponse(ResponseResult response)
        {
            var httpStatusCode = MapToHttpStatusCode(response.StatusCode);

            return new ObjectResult(new
            {
                statusCode = response.StatusCode,
                description = response.Description,
                data = response.ErrorData
            })
            {
                StatusCode = httpStatusCode
            };
        }

        protected IActionResult ProcessResponse<T>(ResponseResult<T> response)
        {
            var httpStatusCode = MapToHttpStatusCode(response.StatusCode);

            return new ObjectResult(new
            {
                statusCode = response.StatusCode,
                description = response.Description,
                data = response.Data
            })
            {
                StatusCode = httpStatusCode
            };
        }

        private static int MapToHttpStatusCode(string responseCode)
        {
            return responseCode switch
            {
                // ── 2xx Success ────────────────────────────────────────
                ResponseCodes.SUCCESS => 200,  // OK
                ResponseCodes.CREATED => 201,  // Created
                ResponseCodes.ACCEPTED => 202,  // Accepted
                ResponseCodes.NO_CONTENT => 204,  // No Content

                ResponseCodes.INVALID_ACTION or
                ResponseCodes.INVALID_REFERENCE_PROVIDED or
                ResponseCodes.WRONG_METHOD_CALL or
                ResponseCodes.INVALID_SECRET_KEY or
                ResponseCodes.ACTION_FAILED => 400,  // Bad Request

                ResponseCodes.INVALID_TOKEN => 401,  // Unauthorized
                ResponseCodes.ACTION_NOT_PERMITTED => 403,  // Forbidden
                ResponseCodes.UNABLE_TO_LOCATE_RECORD => 404,  // Not Found
                ResponseCodes.METHOD_NOT_ALLOWED => 405,  // Method Not Allowed
                ResponseCodes.CONFLICT or
                ResponseCodes.DUPLICATE_RECORD => 409,  // Conflict
                ResponseCodes.UNPROCESSABLE => 422,  // Unprocessable Entity
                ResponseCodes.LOCKED => 423,  // Locked
                ResponseCodes.LIMIT_EXCEEDED => 429,  // Too Many Requests
                ResponseCodes.TIMEOUT => 408,  // Request Timeout

                // ── 5xx Server Errors ──────────────────────────────────
                ResponseCodes.SYSTEM_MALFUNCTION or
                ResponseCodes.ROUTING_ERROR => 500,  // Internal Server Error
                ResponseCodes.NOT_IMPLEMENTED => 501,  // Not Implemented
                ResponseCodes.SERVICE_UNAVAILABLE => 503,  // Service Unavailable
                ResponseCodes.GATEWAY_TIMEOUT => 504,  // Gateway Timeout

                // ── Fallback ───────────────────────────────────────────
                _ => 500
            };
        }
    }
}
