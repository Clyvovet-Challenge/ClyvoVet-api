using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClyvoVet.Api.Filters;

public class ApiKeyFilterAttribute : IActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly string _configKey;

    public ApiKeyFilterAttribute(IConfiguration configuration, string configKey)
    {
        _configuration = configuration;
        _configKey = configKey;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var apiKeyEsperada = _configuration[_configKey];

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var apiKeyRecebida) ||
            apiKeyRecebida != apiKeyEsperada)
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
