using System.Text.Json;
using ClyvoVet.Api.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// Fora de Development o console é o log que o Render e a Azure guardam; em JSON, cada propriedade
/// (CorrelationId, StatusCode…) vira campo pesquisável em vez de texto para garimpar.
/// </summary>
public class ObservabilidadeTests
{
    private static IHostEnvironment Ambiente(string nome)
    {
        var ambiente = new Mock<IHostEnvironment>();
        ambiente.SetupGet(a => a.EnvironmentName).Returns(nome);
        return ambiente.Object;
    }

    // O pacote base do Serilog não tem sink de TextWriter: captura os eventos e formata à mão.
    private sealed class Captura : ILogEventSink
    {
        public List<LogEvent> Eventos { get; } = [];
        public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
    }

    private static JsonElement[] FormatarEmProducao(Action<Serilog.ILogger> escrever)
    {
        var captura = new Captura();
        using (var logger = new LoggerConfiguration()
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty("Application", "ClyvoVet.Api")
                   .WriteTo.Sink(captura)
                   .CreateLogger())
        {
            escrever(logger);
        }

        var formatador = ObservabilidadeExtensions.FormatadorJsonDoConsole(Ambiente("Production"))!;
        var saida = new StringWriter();
        foreach (var evento in captura.Eventos)
            formatador.Format(evento, saida);

        return saida.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(linha => JsonDocument.Parse(linha).RootElement)
            .ToArray();
    }

    [Fact]
    public void FormatadorJsonDoConsole_EmDevelopment_DevolveNulo()
    {
        // Arrange
        var ambiente = Ambiente("Development");

        // Act
        var formatador = ObservabilidadeExtensions.FormatadorJsonDoConsole(ambiente);

        // Assert
        // Nulo = o console continua com o template legível (e as cores) de hoje.
        Assert.Null(formatador);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    [InlineData("Staging")]
    public void FormatadorJsonDoConsole_ForaDeDevelopment_DevolveJsonCompacto(string nomeDoAmbiente)
    {
        // Arrange
        var ambiente = Ambiente(nomeDoAmbiente);

        // Act
        var formatador = ObservabilidadeExtensions.FormatadorJsonDoConsole(ambiente);

        // Assert
        Assert.IsType<CompactJsonFormatter>(formatador);
    }

    [Fact]
    public void FormatadorJsonDoConsole_EventoComPropriedades_EscreveUmaLinhaDeJsonComOsCampos()
    {
        // Arrange & Act
        var linhas = FormatarEmProducao(logger =>
        {
            using (LogContext.PushProperty("CorrelationId", "abc-123"))
                logger.Information("HTTP {RequestMethod} {RequestPath} respondeu {StatusCode}", "GET", "/api/v1/produtos", 200);
        });

        // Assert
        var evento = Assert.Single(linhas);
        Assert.True(evento.TryGetProperty("@t", out _));
        Assert.Equal("HTTP {RequestMethod} {RequestPath} respondeu {StatusCode}", evento.GetProperty("@mt").GetString());
        Assert.Equal("/api/v1/produtos", evento.GetProperty("RequestPath").GetString());
        Assert.Equal(200, evento.GetProperty("StatusCode").GetInt32());
        Assert.Equal("abc-123", evento.GetProperty("CorrelationId").GetString());
        Assert.Equal("ClyvoVet.Api", evento.GetProperty("Application").GetString());
        // Information é o nível implícito do formato compacto: ele não escreve "@l".
        Assert.False(evento.TryGetProperty("@l", out _));
    }

    [Fact]
    public void FormatadorJsonDoConsole_EventoDeErro_TrazNivelEExcecao()
    {
        // Arrange & Act
        var linhas = FormatarEmProducao(logger =>
            logger.Error(new InvalidOperationException("boom"), "Falha ao processar {AnimalId}", "animal-1"));

        // Assert
        var evento = Assert.Single(linhas);
        Assert.Equal("Error", evento.GetProperty("@l").GetString());
        Assert.Contains("InvalidOperationException: boom", evento.GetProperty("@x").GetString());
        Assert.Equal("animal-1", evento.GetProperty("AnimalId").GetString());
    }
}
