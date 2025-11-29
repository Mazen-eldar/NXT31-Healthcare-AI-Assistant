using Base.API.DTOs;
using Base.Services.Implementations;
using Base.Shared.Responses.Exceptions;
using System.Net;
using System.Text.Json;

namespace Base.API.MiddleWare
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.Clear(); // ← مهم جدًا
            context.Response.ContentType = "application/json";

            var includeDetails = _env.IsDevelopment();
            var statusCode = ex switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                BadRequestException => (int)HttpStatusCode.BadRequest,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var apiError = ApiErrorResponseDTO.FromException(ex, statusCode/*, includeDetails*/);
            apiError.TraceId = context.TraceIdentifier;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            await context.Response.WriteAsync(JsonSerializer.Serialize(apiError, options));
        }

    }
}
