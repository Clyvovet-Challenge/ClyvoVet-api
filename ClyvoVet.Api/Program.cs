using System.Reflection;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.HealthChecks;
using ClyvoVet.Api.Middleware;
using ClyvoVet.Api.Repositories;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using ClyvoVet.Api.Swagger;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
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
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", ServiceName)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/clyvovet-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "🐾 Clyvo Vet API",
        Version = "v1",
        Description = """
            API REST de gerenciamento veterinário — domínio **.NET** (ASP.NET Core 8 + Oracle).

            ---

            ### Recursos gerenciados por esta API

            | Recurso | Rota base | Tabela Oracle |
            |---------|-----------|---------------|
            | Produtos | `/api/v1/produtos` | `t_clyvo_produto` |
            | Eventos Pet | `/api/v1/eventos-pet` | `t_clyvo_evento_pet` |
            | Lembretes | `/api/v1/lembretes` | `t_clyvo_lembrete` |
            | Sugestões de Produto | `/api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |

            ### Tabelas lidas da API Java (somente consulta)

            | Tabela | Finalidade |
            |--------|-----------|
            | `t_clyvo_animal` | Validação de `animalId` nas FKs |
            | `t_clyvo_tutor` | JOIN automático pelo EF Core nas respostas enriquecidas |

            ---

            **Banco de dados:** Oracle FIAP — `oracle.fiap.com.br:1521/ORCL`
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
            var other         => [other ?? "Outros"]
        });

    // Descrições por grupo de tag
    options.DocumentFilter<TagDescriptionsDocumentFilter>();

    // Ordena as rotas pelo caminho relativo
    options.OrderActionsBy(api => $"{api.RelativePath}_{api.HttpMethod}");

    // Inclui comentários XML gerados a partir dos /// dos controllers
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<IProdutoRepository,         ProdutoRepository>();
builder.Services.AddScoped<ISugestaoProdutoRepository, SugestaoProdutoRepository>();
builder.Services.AddScoped<ILembreteRepository,        LembreteRepository>();
builder.Services.AddScoped<IEventoPetRepository,       EventoPetRepository>();
builder.Services.AddScoped<IAnimalRepository,          AnimalRepository>();
builder.Services.AddScoped<IPredisposicaoSaudeRepository, PredisposicaoSaudeRepository>();

builder.Services.AddScoped<IProdutoService,         ProdutoService>();
builder.Services.AddScoped<ISugestaoProdutoService, SugestaoProdutoService>();
builder.Services.AddScoped<ILembreteService,        LembreteService>();
builder.Services.AddScoped<IEventoPetService,       EventoPetService>();
builder.Services.AddScoped<IWidgetSaudePreditivaService, WidgetSaudePreditivaService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

// Health Checks — "self" cobre liveness (processo respondendo) e "oracle-database"
// cobre readiness + disponibilidade do serviço externo (Oracle FIAP fica fora do processo,
// em oracle.fiap.com.br), validando a conectividade real via Database.CanConnectAsync().
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: ["live"])
    .AddDbContextCheck<AppDbContext>(
        name: "oracle-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "database", "external"]);

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

        context.Response.StatusCode = exception switch
        {
            NotFoundException   => StatusCodes.Status404NotFound,
            BadRequestException => StatusCodes.Status400BadRequest,
            _                   => StatusCodes.Status500InternalServerError
        };

        var message = exception switch
        {
            NotFoundException   e => e.Message,
            BadRequestException e => e.Message,
            _                     => "Erro interno no servidor."
        };

        switch (exception)
        {
            case NotFoundException or BadRequestException:
                Log.Warning("Requisição inválida em {Path}: {Message}", context.Request.Path, message);
                break;
            default:
                Log.Error(exception, "Erro não tratado em {Path}", context.Request.Path);
                break;
        }

        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// /health           → todos os checks (visão geral, uso no README/monitoramento manual)
// /health/live       → apenas "self" — o processo está de pé (liveness probe)
// /health/ready      → "oracle-database" — o banco Oracle (externo) está acessível (readiness probe)
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
