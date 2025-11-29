using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    /*public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; } // Optional for programmatic handling
    }*/

    /// <summary>
    /// Standardized API response wrapper - Used across the entire application
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Indicates whether the request was successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message (localized if needed)
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Machine-readable error code for frontend handling (e.g., "INVALID_OTP", "ACCOUNT_LOCKED")
        /// </summary>
        [JsonPropertyName("errorCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Optional data payload (only present on success)
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }

        /// <summary>
        /// Timestamp of the response (UTC)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional trace ID for distributed tracing (Correlation ID)
        /// </summary>
        [JsonPropertyName("traceId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; set; }

        // ═══════════════════════════════════════════════════════════
        // Factory Methods - أهم حاجة في الدنيا
        // ═══════════════════════════════════════════════════════════

        public static ApiResponse Ok(string message = "Operation completed successfully.", object? data = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data
            };

        public static ApiResponse Created(string message = "Resource created successfully.", object? data = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data
            };

        public static ApiResponse Fail(string message, string? errorCode = null, object? data = null)
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Data = data
            };

        public static ApiResponse ValidationError(Dictionary<string, string[]> errors)
            => new()
            {
                Success = false,
                Message = "Validation failed.",
                ErrorCode = "VALIDATION_ERROR",
                Data = errors
            };

        public static ApiResponse Unauthorized(string message = "Authentication required or invalid credentials.")
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = "UNAUTHORIZED"
            };

        public static ApiResponse Forbidden(string message = "You don't have permission to perform this action.")
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = "FORBIDDEN"
            };

        public static ApiResponse NotFound(string message = "The requested resource was not found.")
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = "NOT_FOUND"
            };
    }
}
