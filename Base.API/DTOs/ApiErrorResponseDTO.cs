namespace Base.API.DTOs
{
    public class ApiErrorResponseDTO
    {
        /// <summary>
        /// The HTTP status code (e.g. 404, 400, 500).
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// A user-friendly error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Additional details for debugging (usually not shown in production).
        /// </summary>
        public string? Details { get; set; }
        /// <summary>
        /// Helps you find the exact log entry for a user’s failed request
        /// When a client reports an error, they can provide the traceId so you can locate it instantly in logs.
        /// </summary>
        public string? TraceId { get; set; }
        public static ApiErrorResponseDTO FromException(Exception ex, int statusCode, bool includeDetails = false)
        {
            return new ApiErrorResponseDTO
            {
                StatusCode = statusCode,
                Message = ex.Message,
                Details = includeDetails ? ex.ToString() : null
            };
        }

    }
}
