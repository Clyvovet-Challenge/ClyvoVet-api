using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace ClyvoVet.Api.Extensions;

public static class ObservabilidadeExtensions
{
    private const string NomeDoServico = "ClyvoVet.Api";

    private const string TemplateLog =
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// O formatador JSON do console, ou <c>null</c> em Development — lá o template legível continua,
    /// com as cores do terminal. Fora dele o console é o log que o Render e a Azure guardam: uma linha
    /// de JSON por evento deixa CorrelationId, StatusCode e afins pesquisáveis como campos.
    /// </summary>
    public static ITextFormatter? FormatadorJsonDoConsole(IHostEnvironment ambiente) =>
        ambiente.IsDevelopment() ? null : new CompactJsonFormatter();

    public static WebApplicationBuilder AddObservabilidade(this WebApplicationBuilder builder)
    {
        // Configuração estática (em vez do padrão bootstrap-logger/ReloadableLogger): evita o erro
        // "the logger is already frozen" quando o host é construído mais de uma vez no mesmo
        // processo, como acontece com WebApplicationFactory nos testes de integração.
        var configuracaoLog = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", NomeDoServico);

        var formatadorJson = FormatadorJsonDoConsole(builder.Environment);
        if (formatadorJson is null)
            configuracaoLog.WriteTo.Console(outputTemplate: TemplateLog);
        else
            configuracaoLog.WriteTo.Console(formatadorJson);

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
        // nao escreve arquivo -- a suite nao deixa rastro em disco.
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

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: NomeDoServico,
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

        return builder;
    }
}
