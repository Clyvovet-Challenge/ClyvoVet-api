using Serilog.Context;

namespace ClyvoVet.Api.Middleware;

/// <summary>
/// Garante um Correlation ID por requisição (reaproveita o header <c>X-Correlation-Id</c>
/// se o cliente enviar um, ou gera um novo) e injeta no contexto de log do Serilog,
/// permitindo correlacionar todas as linhas de log de uma mesma requisição.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && IsValid(existing.ToString())
            ? existing.ToString()
            : context.TraceIdentifier;

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    // Aceita só letras, dígitos e hífen (cobre GUIDs e trace ids) — um valor enviado pelo
    // cliente sem essa validação seria gravado nos logs e devolvido na resposta como está.
    private static bool IsValid(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
            return false;

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
                return false;
        }

        return true;
    }
}
