using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Base.API.Filters
{
    public class AllowAnonymousOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAllowAnonymous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();

            if (hasAllowAnonymous)
            {
                // Remove the lock icon by clearing security requirements
                operation.Security.Clear();
            }
        }
    }

}
