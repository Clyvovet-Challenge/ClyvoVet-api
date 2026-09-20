using ClyvoVet.Api.Errors;
using ClyvoVet.Api.Extensions;
using ClyvoVet.Api.HealthChecks;
using ClyvoVet.Api.Middleware;
using ClyvoVet.Api.Security;
using ClyvoVet.Application;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog e OpenTelemetry — os comentários do porquê estão em Extensions/ObservabilidadeExtensions.cs.
builder.AddObservabilidade();

// CORS ESPELHANDO O DESENHO DA API JAVA
// Nao afeta o app nativo, que nao faz CORS -- afeta o Expo web, se ele for
// demonstrado, e qualquer chamada a partir do Swagger de outra origem.
//
// As origens vem de configuracao (Cors__Origens no ambiente), como o
// clyvovet.cors.origens do lado Java. NUNCA AllowAnyOrigin: alem de liberar
// geral, ele e incompativel com credenciais, e o navegador recusa a resposta
// em silencio quando os dois aparecem juntos.
const string PoliticaCors = "clyvovet";
var origensPermitidas = (builder.Configuration["Cors:Origens"]
        ?? "http://localhost:3000,http://localhost:8081")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddPolicy(PoliticaCors, policy => policy
        .WithOrigins(origensPermitidas)
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        // X-Api-Key porque e como esta API autentica hoje; X-Correlation-Id
        // porque o CorrelationIdMiddleware aceita o id vindo do cliente.
        .WithHeaders("Authorization", "Content-Type", "X-Api-Key", "X-Correlation-Id")
        .WithExposedHeaders("X-Correlation-Id")
        .SetPreflightMaxAge(TimeSpan.FromHours(1))));

builder.Services.AddControllers();
builder.Services.AddDocumentacaoApi();

builder.Services.AddApplication();

// "self" cobre liveness. Os checks de dependência (banco e Telegram) vêm da Infrastructure,
// junto de quem os implementa, e são registrados DEPOIS deste: a ordem do JSON de /health
// continua sendo a de sempre — self, mysql-database, telegram-bot.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: ["live"]);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Validacao do token emitido pela API Java. Singleton porque a chave e montada
// uma vez; INERTE enquanto Jwt:Secret nao existir, e incapaz de lancar no boot
// mesmo com valor invalido — ver o comentario da classe.
builder.Services.AddSingleton<ValidadorDeTokenJwt>();

// JWT do ASP.NET e politicas de acesso; o porque esta em Extensions/AutenticacaoJwtExtensions.cs.
builder.Services.AddAutenticacaoJwt();

// Quem le a identidade da requisicao corrente e o UsuarioAtualHttp, entao ele
// precisa do acessor -- que NAO vinha registrado. Sem ele a resolucao falha no
// primeiro request, e nao no startup: o app sobe verde e so quebra quando alguem chama.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();

var app = builder.Build();

// CorrelationIdMiddleware precisa envolver o UseSerilogRequestLogging: o log de conclusão da
// requisição só herda o CorrelationId do LogContext enquanto esse escopo ainda está aberto.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseDocumentacaoApi();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        context.Response.ContentType = "application/json";

        // A decisao vive em MapaDeErro, fora deste lambda, para poder ser
        // testada: o caso do 409 depende de chave estrangeira real, e os testes
        // de integracao rodam em InMemory, que nao aplica FK.
        context.Response.StatusCode = MapaDeErro.Status(exception);

        var message = MapaDeErro.Mensagem(exception);

        // O mesmo id que o CorrelationIdMiddleware já colocou no header e que o
        // Serilog imprime em cada linha desta requisição.
        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
        var referencia = MapaDeErro.Referencia(exception, correlationId);

        switch (exception)
        {
            case NotFoundException or BadRequestException or SemTutorNoTokenException:
                Log.Warning("Requisição inválida em {Path}: {Message}", context.Request.Path, message);
                break;
            default:
                Log.Error(exception, "Erro não tratado em {Path}", context.Request.Path);
                break;
        }

        // `error` continua onde estava: é o que o aplicativo lê hoje, e mexer
        // nele quebraria a tela sem ganho nenhum. `referencia` entra ao lado, e
        // só existe quando é falha de servidor — é o campo que permite ao
        // usuário dizer QUAL erro aconteceu, com o mesmo nome que a API Java usa.
        if (referencia is null)
        {
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new { error = message, referencia });
        }
    });
});

app.UseHttpsRedirection();
// Le o Bearer, quando houver, e guarda a identidade em HttpContext.Items.
// NAO rejeita nada — atravessa /health, /metrics, /swagger e os webhooks sem
// tocar neles. Fica depois do CORS para que o preflight OPTIONS nao passe por
// aqui, e antes dos controllers, que sao quem consome a identidade.
app.UseMiddleware<IdentidadeMiddleware>();

// Antes de UseAuthentication e UseAuthorization: o preflight OPTIONS chega sem credencial
// nenhuma e precisa ser respondido pelo CORS, nao recusado pela autenticacao nem pela autorizacao.
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// /health           → todos os checks (visão geral, uso no README/monitoramento manual)
// /health/live       → apenas "self" — o processo está de pé (liveness probe)
// /health/ready      → "mysql-database" — o banco está acessível (readiness probe)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckJsonWriter.WriteResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckJsonWriter.WriteResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckJsonWriter.WriteResponse
});

// Métricas no formato Prometheus (tempo de resposta, contagem de requisições, taxa de erro por status code).
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Necessário para o WebApplicationFactory<Program> localizar o entry point nos testes de integração.
public partial class Program { }
