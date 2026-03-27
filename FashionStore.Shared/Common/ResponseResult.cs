using System.Text.Json.Serialization;
using FashionStore.Shared.Constants;

namespace FashionStore.Shared.Common
{
    public struct ResponseResult
    {
        private string _statusCode;

        private string _message;

        private bool _initialized;

        [JsonIgnore]
        public object ErrorData { get; set; }

        [JsonIgnore]
        public bool IsSuccessful;

        [JsonPropertyName("statusCode")]
        public string StatusCode
        {
            get
            {
                if (!_initialized)
                {
                    return ResponseCodes.SYSTEM_MALFUNCTION;
                }

                return _statusCode;
            }
            set
            {
                _statusCode = value;
                _initialized = true;
            }
        }

        [JsonPropertyName("description")]
        public string Description
        {
            get
            {
                if (!_initialized)
                {
                    return ResponseCodeDescriptions.GetDescription(ResponseCodes.SYSTEM_MALFUNCTION);
                }

                return _message;
            }
            set
            {
                _message = value;
                _initialized = true;
            }
        }

        [JsonPropertyName("data")]
        public object Data { get; private set; }

        public T GetError<T>()
        {
            object errorData = ErrorData;
            if (errorData is T)
            {
                return (T)errorData;
            }

            return default(T);
        }


        public ResponseResult Success(string message = "")
        {
            StatusCode = ResponseCodes.SUCCESS;
            Description = message;
            IsSuccessful = true;
            _initialized = true;
            return this;
        }

        public ResponseResult Fail(string message = null, string responseCode = ResponseCodes.SYSTEM_MALFUNCTION, object errorData = null)
        {
            Description = string.IsNullOrEmpty(message)
                ? ResponseCodeDescriptions.GetDescription(ResponseCodes.SYSTEM_MALFUNCTION)
                : message;
            this.StatusCode = responseCode;
            IsSuccessful = false;
            ErrorData = errorData;
            _initialized = true;
            return this;
        }
    }

    public struct ResponseResult<T>
    {
        private ResponseResult _responseInfo;

        [JsonPropertyName("statusCode")]
        public string StatusCode
        {
            get
            {
                return _responseInfo.StatusCode;
            }
            set
            {
                _responseInfo.StatusCode = value;
            }
        }

        [JsonPropertyName("description")]
        public string Description
        {
            get
            {
                return _responseInfo.Description;
            }
            set
            {
                _responseInfo.Description = value;
            }
        }

        [JsonPropertyName("data")]
        public T Data { get; private set; }

        [JsonIgnore]
        public bool IsSuccessful => _responseInfo.IsSuccessful;

        [JsonIgnore]
        public ResponseResult InfoResult => _responseInfo;

        [JsonIgnore]
        public object ErrorData
        {
            get
            {
                return _responseInfo.ErrorData;
            }
            set
            {
                _responseInfo.ErrorData = value;
            }
        }


        public ResponseResult<T> Success(T result, string message = null!)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result", "result cannot be null when calling ResponseInfo<>.Success(" + typeof(T).FullName + ", string)");
            }

            _responseInfo.Success(message);
            Data = result;
            return this;
        }

        public ResponseResult<T> Fail(string message = "", string responseCode = ResponseCodes.SYSTEM_MALFUNCTION, object errorData = null, T result = default(T))
        {
            Data = result;
            _responseInfo.Fail(message, responseCode, errorData);
            return this;
        }

        public ResponseResult<T> Fail(ResponseResult response, T result = default!)
        {
            Data = result;
            _responseInfo = response;
            return this;
        }

        public TK GetError<TK>()
        {
            return _responseInfo.GetError<TK>();
        }

        public ResponseResult<T> SetStatusCode(string statusCode)
        {
            _responseInfo.StatusCode = statusCode;
            return this;
        }
    }

}
