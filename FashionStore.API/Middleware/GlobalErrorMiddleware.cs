using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using FashionStore.Shared.Common;
using FashionStore.Shared.Constants;
using Microsoft.IdentityModel.Tokens;

namespace FashionStore.API.Middleware
{
    public class GlobalErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorMiddleware> _logger;

        public GlobalErrorMiddleware(RequestDelegate next, ILogger<GlobalErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, responseCode, message, errorData, logLevel) = exception switch
            {
                ValidationException validationException => (
                    StatusCodes.Status400BadRequest,
                    ResponseCodes.UNPROCESSABLE,
                    validationException.Message,
                    (object?)validationException.ValidationResult?.MemberNames,
                    LogLevel.Warning),

                ArgumentException argumentException => (
                    StatusCodes.Status400BadRequest,
                    ResponseCodes.INVALID_ACTION,
                    argumentException.Message,
                    null,
                    LogLevel.Warning),

                KeyNotFoundException keyNotFoundException => (
                    StatusCodes.Status404NotFound,
                    ResponseCodes.UNABLE_TO_LOCATE_RECORD,
                    keyNotFoundException.Message,
                    null,
                    LogLevel.Warning),

                UnauthorizedAccessException unauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    ResponseCodes.INVALID_TOKEN,
                    unauthorizedAccessException.Message,
                    null,
                    LogLevel.Warning),

                SecurityTokenException securityTokenException => (
                    StatusCodes.Status401Unauthorized,
                    ResponseCodes.INVALID_TOKEN,
                    securityTokenException.Message,
                    null,
                    LogLevel.Warning),

                NotImplementedException notImplementedException => (
                    StatusCodes.Status501NotImplemented,
                    ResponseCodes.NOT_IMPLEMENTED,
                    notImplementedException.Message,
                    null,
                    LogLevel.Warning),

                TimeoutException timeoutException => (
                    StatusCodes.Status408RequestTimeout,
                    ResponseCodes.TIMEOUT,
                    timeoutException.Message,
                    null,
                    LogLevel.Warning),

                OperationCanceledException _ when context.RequestAborted.IsCancellationRequested => (
                    499,
                    ResponseCodes.TIMEOUT,
                    "The request was cancelled by the client.",
                    null,
                    LogLevel.Warning),

                OperationCanceledException operationCanceledException => (
                    StatusCodes.Status408RequestTimeout,
                    ResponseCodes.TIMEOUT,
                    operationCanceledException.Message,
                    null,
                    LogLevel.Warning),

                var ex when IsDbUpdateUniqueViolation(ex, out var constraintMessage) => (
                    StatusCodes.Status409Conflict,
                    ResponseCodes.DUPLICATE_RECORD,
                    constraintMessage,
                    null,
                    LogLevel.Warning),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    ResponseCodes.SYSTEM_MALFUNCTION,
                    ResponseCodeDescriptions.GetDescription(ResponseCodes.SYSTEM_MALFUNCTION),
                    null,
                    LogLevel.Error)
            };

            if (logLevel == LogLevel.Error)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "Handled exception for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
            }

            if (context.Response.HasStarted)
            {
                _logger.LogError("The response has already started, the global error middleware will not overwrite it.");
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ResponseResult().Fail(message, responseCode, errorData);

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = response.StatusCode,
                description = response.Description,
                data = response.ErrorData
            }));
        }

        private static bool IsDbUpdateUniqueViolation(Exception exception, out string message)
        {
            message = null!;

            const string dbUpdateExceptionTypeName = "Microsoft.EntityFrameworkCore.DbUpdateException";
            var isDbUpdate = string.Equals(exception.GetType().FullName, dbUpdateExceptionTypeName, StringComparison.Ordinal)
                              || exception.InnerException is not null && string.Equals(exception.GetType().BaseType?.FullName, dbUpdateExceptionTypeName, StringComparison.Ordinal);

            if (!isDbUpdate && exception.InnerException is null)
                return false;

            var inner = exception.InnerException;
            if (inner is null)
                return false;

            string? constraintName = TryGetConstraintName(inner);

            if (string.IsNullOrWhiteSpace(constraintName) && inner.Message is not null && inner.Message.Contains("UQ_DeliveryRate_ZoneId_MethodId", StringComparison.Ordinal))
            {
                constraintName = "UQ_DeliveryRate_ZoneId_MethodId";
            }

            switch (constraintName)
            {
                case "UQ_DeliveryRate_ZoneId_MethodId":
                    message = "A delivery rate already exists for this zone and method combination. Each zone can only have one rate per delivery method. Please update the existing rate instead.";
                    return true;
                default:
                    if (!string.IsNullOrWhiteSpace(constraintName))
                    {
                        message = $"A duplicate record was detected (constraint: {constraintName}).";
                        return true;
                    }
                    return false;
            }
        }

        private static string? TryGetConstraintName(Exception innerException)
        {
            var constraintNameProp = innerException.GetType().GetProperty("ConstraintName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (constraintNameProp is not null && typeof(string).IsAssignableFrom(constraintNameProp.PropertyType))
            {
                return constraintNameProp.GetValue(innerException) as string;
            }

            if (innerException.Data is not null)
            {
                foreach (var key in innerException.Data.Keys)
                {
                    if (key?.ToString()?.Contains("Constraint", StringComparison.OrdinalIgnoreCase) == true)
                        return innerException.Data[key] as string;
                }
            }

            return null;
        }
    }
}
