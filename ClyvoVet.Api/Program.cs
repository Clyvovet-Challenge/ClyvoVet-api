using System.Reflection;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.HealthChecks;
using ClyvoVet.Api.Middleware;
using ClyvoVet.Api.Repositories;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using ClyvoVet.Api.Security;
using ClyvoVet.Api.Swagger;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;                        // MySqlConnectionStringBuilder (transitivo via Pomelo)
using Microsoft.OpenApi;                      // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;       // DocExpansion
using Telegram.Bot;

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
            "WhatsApp"        => ["WhatsApp"],
            "Telegram"        => ["Telegram"],
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

var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// TETO DE CONEXOES EXPLICITO
// O padrao do MySqlConnector e 100 conexoes POR INSTANCIA. A API Java opera com o
// padrao do HikariCP, 10 -- e ela tem 74 endpoints contra os 24 daqui. Esta API
// podia abrir dez vezes mais conexao que a que recebe mais trafego. Nao era
// dimensionamento: era o padrao que ninguem tocou.
//
// POR QUE AQUI E NAO NO appsettings.json
// Na Azure a connection string vem de app setting e substitui a do arquivo. Um
// teto escrito la nao chegaria a producao, que e exatamente onde ele importa.
//
// Quem precisar de outro valor sobrescreve por Database__MaxPoolSize, sem tocar em
// codigo. Antes de subir, confirme o teto real do servidor com
// SHOW VARIABLES LIKE 'max_connections' -- o Flexible Server e Standard_B1ms, tier
// Burstable, e o limite dele nao se presume pelo tier.
if (!string.IsNullOrWhiteSpace(mysqlConnectionString))
{
    mysqlConnectionString = new MySqlConnectionStringBuilder(mysqlConnectionString)
    {
        MaximumPoolSize = builder.Configuration.GetValue<uint?>("Database:MaxPoolSize") ?? 15,
    }.ConnectionString;
}

// VERSAO FIXA, E NAO AutoDetect.
//
// ServerVersion.AutoDetect ABRE UMA CONEXAO com o banco durante a construcao do
// host -- antes de a aplicacao existir. No App Service isso e uma dependencia de
// BOOT: se o MySQL nao estiver alcancavel naquele instante (banco reiniciando,
// regra de firewall ainda propagando, manutencao do Flexible Server), o processo
// morre na inicializacao e o container entra em ciclo de restart. O sintoma no
// portal e "Application Error", sem nada util no log da aplicacao -- porque a
// aplicacao nunca chegou a subir para logar.
//
// Com a versao declarada, a app sobe mesmo com o banco fora e falha so na
// requisicao que precisa dele -- que e onde o health check /health/ready ja sabe
// reportar o problema.
//
// 8.0 e o que o azure/02-banco-mysql.sh provisiona (MYSQL_VERSION="8.0") e o que
// o docker-compose local usa (mysql:8.0). Se um dia o servidor subir de versao,
// esta linha muda junto -- e essa e a intencao: virar decisao explicita.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0))));

builder.Services.AddScoped<IProdutoRepository,         ProdutoRepository>();
builder.Services.AddScoped<ISugestaoProdutoRepository, SugestaoProdutoRepository>();
builder.Services.AddScoped<ILembreteRepository,        LembreteRepository>();
builder.Services.AddScoped<IEventoPetRepository,       EventoPetRepository>();
builder.Services.AddScoped<IAnimalRepository,          AnimalRepository>();
builder.Services.AddScoped<IPredisposicaoSaudeRepository, PredisposicaoSaudeRepository>();
builder.Services.AddScoped<ITutorTelegramRepository, TutorTelegramRepository>();

builder.Services.AddScoped<IProdutoService,         ProdutoService>();
builder.Services.AddScoped<ISugestaoProdutoService, SugestaoProdutoService>();
builder.Services.AddScoped<ILembreteService,        LembreteService>();
builder.Services.AddScoped<IEventoPetService,       EventoPetService>();
builder.Services.AddScoped<IWidgetSaudePreditivaService, WidgetSaudePreditivaService>();
// Validacao do token emitido pela API Java. Singleton porque a chave e montada
// uma vez; INERTE enquanto Jwt:Secret nao existir, e incapaz de lancar no boot
// mesmo com valor invalido — ver o comentario da classe.
builder.Services.AddSingleton<ValidadorDeTokenJwt>();

// O EscopoDoTutor le a identidade da requisicao corrente, entao precisa do
// acessor -- que NAO vinha registrado. Sem ele a resolucao falha no primeiro
// request, e nao no startup: o app sobe verde e so quebra quando alguem chama.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EscopoDoTutor>();

builder.Services.AddSingleton<IWhatsAppService, WhatsAppService>();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    new TelegramBotClient(sp.GetRequiredService<IConfiguration>()["Telegram:BotToken"]!));
builder.Services.AddSingleton<ITelegramService, TelegramService>();

// Singleton, e nao Scoped: quem GERA o convite e uma requisicao HTTP, quem o CONSOME
// e o BackgroundService do Telegram. Se cada um recebesse a sua instancia, todo link
// nasceria ja invalido.
builder.Services.AddSingleton<VinculosPendentesDeTelegram>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<TelegramLinkListenerService>();
    builder.Services.AddHostedService<LembreteNotificationService>();
}

// Health Checks — "self" cobre liveness (processo respondendo), "mysql-database" cobre
// readiness (Database.CanConnectAsync() contra o MySQL). "telegram-bot" e
// "whatsapp-twilio" verificam os demais serviços externos integrados pela API, mas ficam
// fora da tag "ready": uma instabilidade neles não deveria tirar a API inteira de rotação,
// já que os outros recursos (Produto, Lembrete, EventoPet, SugestaoProduto) continuam
// funcionando normalmente sem Telegram/WhatsApp.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: ["live"])
    .AddDbContextCheck<AppDbContext>(
        name: "mysql-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "database", "external"])
    .AddCheck<TelegramHealthCheck>("telegram-bot", tags: ["external"])
    .AddCheck<WhatsAppHealthCheck>("whatsapp-twilio", tags: ["external"]);

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
            NotFoundException        => StatusCodes.Status404NotFound,
            BadRequestException      => StatusCodes.Status400BadRequest,
            // O recorte esta ligado e a requisicao nao identifica um tutor.
            // 403 e nao 401: a X-Api-Key foi aceita, o que falta e IDENTIDADE.
            SemTutorNoTokenException => StatusCodes.Status403Forbidden,
            _                        => StatusCodes.Status500InternalServerError
        };

        var message = exception switch
        {
            NotFoundException        e => e.Message,
            BadRequestException      e => e.Message,
            SemTutorNoTokenException e => e.Message,
            _                          => "Erro interno no servidor."
        };

        switch (exception)
        {
            case NotFoundException or BadRequestException or SemTutorNoTokenException:
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
