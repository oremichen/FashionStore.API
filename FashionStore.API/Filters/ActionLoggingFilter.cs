using Microsoft.AspNetCore.Mvc.Filters;

namespace FashionStore.API.Filters;

public sealed class ActionLoggingFilter(ILogger<ActionLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";
        var action = context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
        logger.LogInformation("Executing {Controller}.{Action}.", controller, action);

        var executedContext = await next();

        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            logger.LogError(executedContext.Exception, "{Controller}.{Action} failed.", controller, action);
            return;
        }

        logger.LogInformation(
            "Completed {Controller}.{Action} with status code {StatusCode}.",
            controller,
            action,
            context.HttpContext.Response.StatusCode);
    }
}
