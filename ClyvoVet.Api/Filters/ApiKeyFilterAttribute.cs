using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClyvoVet.Api.Filters;

public class ApiKeyFilterAttribute : IActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyFilterAttribute(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var apiKeyEsperada = _configuration["WhatsApp:ApiKey"];

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var apiKeyRecebida) ||
            apiKeyRecebida != apiKeyEsperada)
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
