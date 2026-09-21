using System.Text.Json;
using ClyvoVet.Api.Errors;
using ClyvoVet.Api.Extensions;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O tratador só ESCREVE a resposta; quem decide status e mensagem é o <see cref="MapaDeErro"/>. O
/// que se prova aqui é o contrato com o app: <c>error</c> sempre, <c>referencia</c> só em falha de
/// servidor, e o log no mesmo nível de antes.
/// </summary>
public class TratadorGlobalDeExcecoesTests
{
    private static (TratadorGlobalDeExcecoes Tratador, Mock<ILogger<TratadorGlobalDeExcecoes>> Log, ServiceProvider Servicos) Criar()
    {
        var servicos = new ServiceCollection().AddLogging().AddTratamentoDeExcecoes().BuildServiceProvider();
        var log = new Mock<ILogger<TratadorGlobalDeExcecoes>>();
        var tratador = new TratadorGlobalDeExcecoes(servicos.GetRequiredService<IProblemDetailsService>(), log.Object);
        return (tratador, log, servicos);
    }

    private static DefaultHttpContext Contexto(IServiceProvider servicos, string? accept = null)
    {
        var contexto = new DefaultHttpContext { RequestServices = servicos, TraceIdentifier = "trace-de-teste" };
        contexto.Response.Body = new MemoryStream();
        contexto.Response.Headers["X-Correlation-Id"] = "corr-123";
        if (accept is not null) contexto.Request.Headers.Accept = accept;
        return contexto;
    }

    private static async Task<JsonElement> CorpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        return (await JsonDocument.ParseAsync(contexto.Response.Body)).RootElement;
    }

    public static IEnumerable<object[]> Casos() =>
    [
        [new NotFoundException("Produto com id abc não encontrado."), 404, "Produto com id abc não encontrado."],
        [new BadRequestException("data no passado"), 400, "data no passado"],
        [new SemTutorNoTokenException(), 403, new SemTutorNoTokenException().Message],
        [new RegistroEmUsoException(), 409, "Registro em uso por outro cadastro."],
        [new InvalidOperationException("detalhe interno que nao deve vazar"), 500, "Erro interno no servidor."],
    ];

    [Theory]
    [MemberData(nameof(Casos))]
    public async Task TryHandleAsync_Excecao_EscreveStatusEErrorDoMapa(Exception excecao, int status, string mensagem)
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        var tratada = await tratador.TryHandleAsync(contexto, excecao, CancellationToken.None);

        // Assert
        Assert.True(tratada);
        Assert.Equal(status, contexto.Response.StatusCode);
        var corpo = await CorpoAsync(contexto);
        Assert.Equal(mensagem, corpo.GetProperty("error").GetString());
        Assert.Equal(status, corpo.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task TryHandleAsync_FalhaDeServidor_TrazReferenciaETraceId()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        await tratador.TryHandleAsync(contexto, new InvalidOperationException("banco fora do ar"), CancellationToken.None);

        // Assert
        var corpo = await CorpoAsync(contexto);
        // O mesmo id que já viaja no header e que o Serilog imprime em toda linha da requisição.
        Assert.Equal("corr-123", corpo.GetProperty("referencia").GetString());
        Assert.Equal("trace-de-teste", corpo.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_ErroDoUsuario_NaoTrazReferencia()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        await tratador.TryHandleAsync(contexto, new NotFoundException("x"), CancellationToken.None);

        // Assert
        // O usuário tem o que corrigir: um código ao lado da frase só acrescentaria ruído.
        Assert.False((await CorpoAsync(contexto)).TryGetProperty("referencia", out _));
    }

    [Fact]
    public async Task TryHandleAsync_ClienteQueNaoAceitaJson_AindaRecebeOCorpoComError()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos, accept: "text/html");

        // Act
        var tratada = await tratador.TryHandleAsync(contexto, new NotFoundException("nao achei"), CancellationToken.None);

        // Assert
        // O IProblemDetailsService recusa escrever para quem não aceita JSON; sem o fallback a
        // resposta sairia SEM corpo, e o app perderia o `error` (A9).
        Assert.True(tratada);
        Assert.Equal("nao achei", (await CorpoAsync(contexto)).GetProperty("error").GetString());
    }

    [Theory]
    [InlineData(typeof(NotFoundException), LogLevel.Warning)]
    [InlineData(typeof(BadRequestException), LogLevel.Warning)]
    [InlineData(typeof(SemTutorNoTokenException), LogLevel.Warning)]
    [InlineData(typeof(InvalidOperationException), LogLevel.Error)]
    public async Task TryHandleAsync_Excecao_RegistraNoMesmoNivelDeAntes(Type tipo, LogLevel esperado)
    {
        // Arrange
        var (tratador, log, servicos) = Criar();
        var excecao = tipo == typeof(SemTutorNoTokenException)
            ? new SemTutorNoTokenException()
            : (Exception)Activator.CreateInstance(tipo, "mensagem")!;

        // Act
        await tratador.TryHandleAsync(Contexto(servicos), excecao, CancellationToken.None);

        // Assert
        log.Verify(l => l.Log(
            esperado, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
