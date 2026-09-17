using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FashionStore.API.Middleware;

public sealed class RequestPayloadLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestPayloadLoggingMiddleware> logger)
{
    private const int MaxLoggedPayloadCharacters = 512 * 1024;
    private const long MaxMultipartRequestBytes = 25L * 1024 * 1024;
    private const string RedactedValue = "[REDACTED]";

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsMutationRequest(context.Request.Method))
        {
            if (context.Request.HasFormContentType && context.Request.ContentLength > MaxMultipartRequestBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            object payload;
            try
            {
                payload = await ReadSanitizedPayloadAsync(context.Request, context.RequestAborted);
            }
            catch (IOException exception) when (context.Request.HasFormContentType)
            {
                logger.LogWarning(
                    exception,
                    "Rejected incomplete multipart request for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }
            logger.LogInformation(
                "Incoming mutation request {Method} {Path}. TraceId: {TraceId}. ContentType: {ContentType}. Payload: {@RequestPayload}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                context.Request.ContentType,
                payload);
        }

        await next(context);
    }

    private static bool IsMutationRequest(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    private static async Task<object> ReadSanitizedPayloadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength == 0)
        {
            return "[EMPTY]";
        }

        if (request.HasFormContentType)
        {
            return new
            {
                status = "omitted",
                reason = "multipart-payload-not-logged",
                contentLength = request.ContentLength
            };
        }

        if (request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return new
            {
                status = "omitted",
                reason = "unsupported-content-type",
                contentLength = request.ContentLength
            };
        }

        if (request.ContentLength > MaxLoggedPayloadCharacters)
        {
            return new
            {
                status = "omitted",
                reason = "payload-too-large",
                contentLength = request.ContentLength
            };
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        var buffer = new char[MaxLoggedPayloadCharacters + 1];
        var totalRead = 0;
        using (var reader = new StreamReader(
                   request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: true,
                   leaveOpen: true))
        {
            while (totalRead < buffer.Length)
            {
                var read = await reader.ReadAsync(
                    buffer.AsMemory(totalRead, buffer.Length - totalRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }
        }

        request.Body.Position = 0;
        if (totalRead > MaxLoggedPayloadCharacters)
        {
            return new
            {
                status = "omitted",
                reason = "payload-too-large",
                contentLength = request.ContentLength
            };
        }

        var body = new string(buffer, 0, totalRead);
        if (string.IsNullOrWhiteSpace(body))
        {
            return "[EMPTY]";
        }

        try
        {
            var json = JsonNode.Parse(body);
            Redact(json);
            return json?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
        }
        catch (JsonException)
        {
            return new
            {
                status = "omitted",
                reason = "invalid-json",
                contentLength = request.ContentLength
            };
        }
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitive(property.Key))
                {
                    jsonObject[property.Key] = RedactedValue;
                }
                else
                {
                    Redact(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Redact(item);
            }
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        var normalized = new string(propertyName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return normalized.Contains("password", StringComparison.Ordinal) ||
               normalized.Contains("token", StringComparison.Ordinal) ||
               normalized.Contains("secret", StringComparison.Ordinal) ||
               normalized.Contains("authorization", StringComparison.Ordinal) ||
               normalized.Contains("apikey", StringComparison.Ordinal) ||
               normalized.Contains("cardnumber", StringComparison.Ordinal) ||
               normalized is "cvv" or "cvc" or "pin" or "otp";
    }
}
