using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Presentation.Extensions.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UseApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["ApiKeys:StandardApiKey"];

        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var key))
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, error = "Invalid key or api-key is missing." });
            return;
        }

        if(string.IsNullOrWhiteSpace(apiKey) || !string.Equals(key, apiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, error = "Invalid api-key." });
            return;
        }

        await next();
    }
}
