using ClyvoVet.Api.Middleware;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ClyvoVet.Api.Errors;

/// <summary>
/// Transforma a exceção não tratada em resposta. A DECISÃO (status, mensagem, referência) vive no
/// <see cref="MapaDeErro"/>, fora daqui, para poder ser testada: o caso do 409 depende de chave
/// estrangeira real, e os testes de integração rodam em InMemory, que não aplica FK.
/// </summary>
public class TratadorGlobalDeExcecoes(
    IProblemDetailsService problemas,
    ILogger<TratadorGlobalDeExcecoes> log) : IExceptionHandler
{
    private const string HeaderCorrelationId = "X-Correlation-Id";

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        var status = MapaDeErro.Status(excecao);
        var mensagem = MapaDeErro.Mensagem(excecao);

        // O mesmo id que o CorrelationIdMiddleware colocou no header e que o Serilog imprime em cada
        // linha desta requisição. Vem de Items e não do header da resposta: o ExceptionHandlerMiddleware
        // limpa os headers antes de chamar este tratador, então lá o id já não existe (foi o que
        // deixou a `referencia` sempre ausente, sem que nenhum teste de unidade percebesse).
        var correlationId = contexto.Items[CorrelationIdMiddleware.ChaveDoItem] as string;
        var referencia = MapaDeErro.Referencia(excecao, correlationId);

        // O header some junto com a limpeza; devolvê-lo mantém o que o CORS já expõe para o app.
        if (!string.IsNullOrWhiteSpace(correlationId))
            contexto.Response.Headers[HeaderCorrelationId] = correlationId;

        Registrar(contexto, excecao, mensagem);

        contexto.Response.StatusCode = status;
        var problema = new ProblemDetails { Status = status, Title = ReasonPhrases.GetReasonPhrase(status) };

        // `error` continua onde estava: é o que o aplicativo lê hoje, e mexer nele quebraria a tela
        // sem ganho nenhum. `referencia` entra ao lado, e só existe quando é falha de servidor — é o
        // campo que permite ao usuário dizer QUAL erro aconteceu, com o mesmo nome que a API Java usa.
        problema.Extensions["error"] = mensagem;
        if (referencia is not null)
            problema.Extensions["referencia"] = referencia;

        var escrito = await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excecao,
        });

        // O serviço recusa escrever para quem não aceita JSON (Accept: text/html). Sem este fallback
        // a resposta sairia sem corpo, e o app perderia o `error`.
        if (!escrito)
            await contexto.Response.WriteAsJsonAsync(problema, ct);

        return true;
    }

    private void Registrar(HttpContext contexto, Exception excecao, string mensagem)
    {
        if (excecao is NotFoundException or BadRequestException or SemTutorNoTokenException)
            log.LogWarning("Requisição inválida em {Path}: {Message}", contexto.Request.Path, mensagem);
        else
            log.LogError(excecao, "Erro não tratado em {Path}", contexto.Request.Path);
    }
}
