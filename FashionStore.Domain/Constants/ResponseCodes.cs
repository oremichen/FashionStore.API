namespace FashionStore.Domain.Constants
{
    public static class ResponseCodes
    {
        public const string TIMEOUT = "-1";  // 408
        public const string SYSTEM_MALFUNCTION = "90";  // 500
        public const string ROUTING_ERROR = "18";  // 500
        public const string SUCCESS = "00";  // 200
        public const string CREATED = "16";  // 201
        public const string ACCEPTED = "17";  // 202
        public const string NO_CONTENT = "19";  
        public const string STATUS_UNKNOWN = "01";  
        public const string REQUEST_IN_PROGRESS = "04";  
        public const string NO_ACTION_TAKEN = "08"; 
        public const string INVALID_ACTION = "02";  // 400
        public const string ACTION_FAILED = "03";  // 400
        public const string INVALID_TOKEN = "05";  // 401
        public const string INVALID_REFERENCE_PROVIDED = "06";  // 400
        public const string WRONG_METHOD_CALL = "07";  // 400
        public const string UNABLE_TO_LOCATE_RECORD = "09";  // 404
        public const string DUPLICATE_RECORD = "10";  // 409
        public const string INVALID_SECRET_KEY = "11";  // 400
        public const string ACTION_NOT_PERMITTED = "12";  // 403
        public const string LIMIT_EXCEEDED = "13";  // 429
        public const string SECURITY_VIOLATION = "14";  // 403
        public const string EXCEEDS_WITHDRAWAL_FREQUENCY = "15"; // 429
        public const string METHOD_NOT_ALLOWED = "20";  // 405
        public const string UNPROCESSABLE = "21";  // 422
        public const string LOCKED = "22";  // 423
        public const string CONFLICT = "23";  // 410
        public const string NOT_IMPLEMENTED = "91";  // 501
        public const string SERVICE_UNAVAILABLE = "92";  // 503
        public const string GATEWAY_TIMEOUT = "93";  // 504
    }
}
