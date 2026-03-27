namespace FashionStore.Shared.Constants
{
    public static class ResponseCodeDescriptions
    {
        private static readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>
        {
            { ResponseCodes.TIMEOUT, "Timeout" },
            { ResponseCodes.SYSTEM_MALFUNCTION, "System malfunction" },
            { ResponseCodes.ROUTING_ERROR, "Routing error" },
            { ResponseCodes.SUCCESS, "Action was successful" },
            { ResponseCodes.CREATED, "Resource created successfully" },
            { ResponseCodes.ACCEPTED, "Request accepted for processing" },
            { ResponseCodes.NO_CONTENT, "Request completed successfully with no content" },
            { ResponseCodes.STATUS_UNKNOWN, "Status unknown" },
            { ResponseCodes.REQUEST_IN_PROGRESS, "Request in progress" },
            { ResponseCodes.NO_ACTION_TAKEN, "No action taken" },
            { ResponseCodes.INVALID_ACTION, "Invalid action" },
            { ResponseCodes.ACTION_FAILED, "Action failed" },
            { ResponseCodes.INVALID_TOKEN, "Unauthorized, invalid token provided" },
            { ResponseCodes.INVALID_REFERENCE_PROVIDED, "Invalid reference provided" },
            { ResponseCodes.WRONG_METHOD_CALL, "Wrong method call" },
            { ResponseCodes.UNABLE_TO_LOCATE_RECORD, "Unable to locate record" },
            { ResponseCodes.DUPLICATE_RECORD, "Duplicate record" },
            { ResponseCodes.INVALID_SECRET_KEY, "Invalid secret key" },
            { ResponseCodes.ACTION_NOT_PERMITTED, "Action not permitted" },
            { ResponseCodes.LIMIT_EXCEEDED, "Limit exceeded" },
            { ResponseCodes.SECURITY_VIOLATION, "Security violation" },
            { ResponseCodes.EXCEEDS_WITHDRAWAL_FREQUENCY, "Exceeds withdrawal frequency" },
            { ResponseCodes.METHOD_NOT_ALLOWED, "Method not allowed" },
            { ResponseCodes.UNPROCESSABLE, "Request could not be processed" },
            { ResponseCodes.LOCKED, "Resource is locked" },
            { ResponseCodes.CONFLICT, "Request conflicts with the current state of the resource" },
            { ResponseCodes.NOT_IMPLEMENTED, "Requested functionality is not implemented" },
            { ResponseCodes.SERVICE_UNAVAILABLE, "Service unavailable" },
            { ResponseCodes.GATEWAY_TIMEOUT, "Gateway timeout" }
        };

        public static string GetDescription(string code)
        {
            if (_descriptions.TryGetValue(code, out var description))
                return description;

            return "Unknown response code";
        }
    }
}
