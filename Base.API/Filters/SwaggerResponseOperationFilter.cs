using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Base.API.DTOs;

namespace Base.API.Filters
{
    public class SwaggerResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // هل الـ endpoint مؤمن بـ [Authorize] ؟
            var hasAuthorize =
                context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
                context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            // ✅ Responses موحدة
            AddJsonResponse(operation, "200", "Request completed successfully", typeof(ApiResponseDTO), GetExample(200));
            AddJsonResponse(operation, "201", "Resource created successfully", typeof(ApiResponseDTO), GetExample(201));
            AddJsonResponse(operation, "400", "Bad request - validation or logic error", typeof(ApiErrorResponseDTO), GetExample(400));
            AddJsonResponse(operation, "404", "Resource not found", typeof(ApiErrorResponseDTO), GetExample(404));

            if (hasAuthorize)
            {
                AddJsonResponse(operation, "401", "Unauthorized - authentication required", typeof(ApiErrorResponseDTO), GetExample(401));
                AddJsonResponse(operation, "403", "Forbidden - insufficient permissions", typeof(ApiErrorResponseDTO), GetExample(403));
            }

            AddJsonResponse(operation, "500", "Server error - unexpected failure", typeof(ApiErrorResponseDTO), GetExample(500));
        }

        // ✅ إضافة Response مع التأكد من أنه JSON
        private static void AddJsonResponse(OpenApiOperation operation, string code, string description, Type type, IOpenApiAny example)
        {
            if (!operation.Responses.ContainsKey(code))
            {
                operation.Responses.Add(code, new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        // مهم: نخلي content type JSON دايمًا
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = "object" }, // schema عام
                            Example = example
                        }
                    }
                });
            }
            else
            {
                // لو موجود response، نأكد إن content type موجود ومضاف example
                var response = operation.Responses[code];
                if (!response.Content.ContainsKey("application/json"))
                {
                    response.Content["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema { Type = "object" },
                        Example = example
                    };
                }
                else
                {
                    response.Content["application/json"].Example = example;
                }
            }
        }

        // 🎨 أمثلة JSON جاهزة لكل كود حالة
        private static IOpenApiAny GetExample(int statusCode)
        {
            return statusCode switch
            {
                200 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(200),
                    //["message"] = new OpenApiString("Success"),
                    //["data"] = new OpenApiString("Email OTP sent"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000001")
                },
                201 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(201),
                    //["message"] = new OpenApiString("Resource created successfully"),
                    //["data"] = new OpenApiObject
                    //{
                    //    ["id"] = new OpenApiInteger(101),
                    //    ["name"] = new OpenApiString("New Resource")
                    //},
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000002")
                },
                400 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(400),
                    //["message"] = new OpenApiString("Validation failed for field"),
                    //["details"] = new OpenApiString("field is required"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000003")
                },
                401 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(401),
                    //["message"] = new OpenApiString("Invalid credentials"),
                    //["details"] = new OpenApiString("Token expired or invalid"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000004")
                },
                403 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(403),
                    //["message"] = new OpenApiString("Forbidden"),
                    //["details"] = new OpenApiString("Insufficient permissions"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000005")
                },
                404 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(404),
                    //["message"] = new OpenApiString("Resource not found"),
                    //["details"] = new OpenApiString("The requested resource could not be found"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000006")
                },
                500 => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(500),
                    //["message"] = new OpenApiString("Internal server error"),
                    //["details"] = new OpenApiString("An unexpected error occurred"),
                    //["traceId"] = new OpenApiString("0HMB2N7G1D9L9:00000007")
                },
                _ => new OpenApiObject
                {
                    ["statusCode"] = new OpenApiInteger(statusCode),
                    //["message"] = new OpenApiString("Generic Response Example"),
                    //["data"] = new OpenApiNull(),
                    //["traceId"] = new OpenApiString("TRACE-ID-HERE")
                }
            };
        }
    }
}
