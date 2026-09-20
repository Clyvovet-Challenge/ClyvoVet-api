using ClyvoVet.Api.Security;
using ClyvoVet.Application;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Api.Errors;
using System.Reflection;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.HealthChecks;
using ClyvoVet.Api.Middleware;
using ClyvoVet.Application.Security;
using ClyvoVet.Api.Swagger;
using ClyvoVet.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;                      // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;       // DocExpansion

const string ServiceName = "ClyvoVet.Api";

var builder = WebApplication.CreateBuilder(args);

// Configuração estática (em vez do padrão bootstrap-logger/ReloadableLogger): evita o erro
// "the logger is already frozen" quando o host é construído mais de uma vez no mesmo
// processo, como acontece com WebApplicationFactory nos testes de integração.
const string TemplateLog =
    "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}";

var configuracaoLog = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", ServiceName)
    .WriteTo.Console(outputTemplate: TemplateLog);

// SINK DE ARQUIVO SO EM DESENVOLVIMENTO
// O requisito da disciplina pede "saida para console/arquivo". O console atende a
// leitura natural ("console ou arquivo"), mas o arquivo existir no codigo tira a
// duvida -- e da o que demonstrar rodando local, que e onde ele serve para algo.
//
// Fora de Development ele nao entra, e o motivo e concreto: no App Service o
// caminho e efemero e por instancia. Cada replica escreveria o seu proprio
// arquivo, ninguem os agrega, e o conteudo some no proximo restart -- seria a
// unica dependencia de armazenamento local em qualquer das duas APIs. Lá quem
// captura o log e o console, via "Log stream" e Application Insights.
//
// O ambiente de teste e "Testing" (IntegrationTestFixture), entao a suite tambem
// nao escreve arquivo -- 113 testes nao deixam rastro em disco.
if (builder.Environment.IsDevelopment())
{
    configuracaoLog.WriteTo.File(
        path: "Logs/clyvovet-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: TemplateLog);
}

Log.Logger = configuracaoLog.CreateLogger();

builder.Host.UseSerilog();

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
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "🐾 Clyvo Vet API",
        Version = "v1",
        Description = """
            API REST de gerenciamento veterinário — domínio **.NET** (ASP.NET Core 8 + MySQL).

            ---

            ### Recursos gerenciados por esta API

            | Recurso | Rota base | Tabela |
            |---------|-----------|---------------|
            | Produtos | `/api/v1/produtos` | `t_clyvo_produto` |
            | Eventos Pet | `/api/v1/eventos-pet` | `t_clyvo_evento_pet` |
            | Lembretes | `/api/v1/lembretes` | `t_clyvo_lembrete` |
            | Sugestões de Produto | `/api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |

            ### Tabelas da API Java (somente consulta)

            | Tabela | Finalidade |
            |--------|-----------|
            | `animal` | Validação de `animalId` nas FKs |
            | `tutor` | JOIN automático pelo EF Core nas respostas enriquecidas |

            > Nesta entrega (Sprint 3 — DevOps Tools & Cloud Computing), o banco é um Azure Database
            > for MySQL Flexible Server **compartilhado com a API Java** — as tabelas `tutor` e `animal`
            > seguem o schema definido pelas migrations Flyway do time de Java.

            ---

            **Banco de dados:** Azure Database for MySQL Flexible Server
            """,
        Contact = new OpenApiContact
        {
            Name  = "Clyvo Vet — Equipe .NET",
            Email = "rm562312@fiap.com.br"
        }
    });

    // Agrupa por controller com nomes amigáveis
    options.TagActionsBy(api =>
        api.ActionDescriptor.RouteValues["controller"] switch
        {
            "Produto"         => ["Produtos"],
            "Lembrete"        => ["Lembretes"],
            "EventoPet"       => ["Eventos Pet"],
            "SugestaoProduto" => ["Sugestões de Produto"],
            "WidgetSaudePreditiva" => ["Widget de Saúde Preditiva"],
            "SaudePreditiva"  => ["Saúde Preditiva (IA)"],
            "Telegram"        => ["Telegram"],
            var other         => [other ?? "Outros"]
        });

    // Descrições por grupo de tag
    options.DocumentFilter<TagDescriptionsDocumentFilter>();

    // Ordena as rotas pelo caminho relativo
    options.OrderActionsBy(api => $"{api.RelativePath}_{api.HttpMethod}");

    // Um XML por projeto que tem documentação para o Swagger: os controllers estão na Api,
    // e os DTOs — com o summary de cada campo — na Application. Sem o segundo, o Swagger
    // continua respondendo 200 e a descrição dos campos some em silêncio.
    foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(ProdutoRequest).Assembly })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    }

    // Botão "Authorize" no Swagger — os endpoints principais (Produto, Lembrete,
    // EventoPet, SugestaoProduto) exigem o header X-Api-Key. O cadeado só aparece
    // nesses endpoints (ver ApiKeySecurityOperationFilter) — Widget não exige
    // chave, e WhatsApp/Telegram exigem chaves próprias e diferentes desta.
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name        = "X-Api-Key",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Description = "Chave de API exigida pelos endpoints principais da Sprint 3."
    });
    options.DocumentFilter<ApiKeySecurityDocumentFilter>();
});

builder.Services.AddApplication();

// Validacao do token emitido pela API Java. Singleton porque a chave e montada
// uma vez; INERTE enquanto Jwt:Secret nao existir, e incapaz de lancar no boot
// mesmo com valor invalido — ver o comentario da classe.
builder.Services.AddSingleton<ValidadorDeTokenJwt>();

// Quem le a identidade da requisicao corrente e o UsuarioAtualHttp, entao ele
// precisa do acessor -- que NAO vinha registrado. Sem ele a resolucao falha no
// primeiro request, e nao no startup: o app sobe verde e so quebra quando alguem chama.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();

// "self" cobre liveness. Os checks de dependência (banco e Telegram) vêm da Infrastructure,
// junto de quem os implementa, e são registrados DEPOIS deste: a ordem do JSON de /health
// continua sendo a de sempre — self, mysql-database, telegram-bot.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: ["live"]);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: ServiceName,
        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// CorrelationIdMiddleware precisa envolver o UseSerilogRequestLogging: o log de conclusão da
// requisição só herda o CorrelationId do LogContext enquanto esse escopo ainda está aberto.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Swagger sempre ativo — professor pode testar sem cliente HTTP externo
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Clyvo Vet API v1");
    options.RoutePrefix               = "swagger";
    options.DocumentTitle             = "Clyvo Vet — API de Gestão Veterinária";
    options.DefaultModelsExpandDepth(-1);            // oculta seção Schemas por padrão
    options.DocExpansion(DocExpansion.List);          // lista endpoints recolhidos
    options.DisplayRequestDuration();                 // exibe tempo de resposta em cada chamada
    options.EnableFilter();                           // caixa de busca/filtro de rotas
    options.EnableDeepLinking();                      // URLs navegáveis por endpoint (bookmark)
    options.EnableTryItOutByDefault();                // "Try it out" já aberto por padrão
});

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

// Antes de UseAuthorization: o preflight OPTIONS chega sem credencial nenhuma e
// precisa ser respondido pelo CORS, nao recusado pela autorizacao.
app.UseCors(PoliticaCors);
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
