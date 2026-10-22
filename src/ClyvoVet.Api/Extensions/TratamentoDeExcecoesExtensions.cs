using System.Diagnostics;
using ClyvoVet.Api.Errors;

namespace ClyvoVet.Api.Extensions;

public static class TratamentoDeExcecoesExtensions
{
    public static IServiceCollection AddTratamentoDeExcecoes(this IServiceCollection services)
    {
        // traceId: o mesmo que o OpenTelemetry usa para a requisição (design §6.1).
        services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
            contexto.ProblemDetails.Extensions.TryAdd(
                "traceId", Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier));

        services.AddExceptionHandler<TratadorGlobalDeExcecoes>();
        return services;
    }
}
