using System.Net;
using System.Text.Json;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// O contrato "falha de servidor traz <c>referencia</c>" pelo pipeline real. O <c>MapaDeErroTests</c>
/// e o <c>TratadorGlobalDeExcecoesTests</c> provam a decisão e a escrita, mas montando o contexto à
/// mão — e é justamente o pipeline que limpa os headers da resposta antes de chamar o tratador, o
/// que deixava a <c>referencia</c> sempre ausente sem que nenhum teste percebesse.
/// </summary>
public class ReferenciaDeErroEndpointsTests : IClassFixture<AutenticacaoJwtFixture>
{
    private const string Header = "X-Correlation-Id";

    private readonly AutenticacaoJwtFixture _fixture;

    public ReferenciaDeErroEndpointsTests(AutenticacaoJwtFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FalhaDeServidor_ClienteEnviaCorrelationId_CorpoEHeaderTrazemOMesmoId()
    {
        // Arrange
        var cliente = _fixture.CreateClient();
        cliente.DefaultRequestHeaders.Add(Header, "minha-ref-123");

        // Act
        var resposta = await cliente.GetAsync("/teste-erro/servidor");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("minha-ref-123", corpo.GetProperty("referencia").GetString());
        Assert.Equal("minha-ref-123", resposta.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task FalhaDeServidor_SemCorrelationIdDoCliente_ReferenciaIgualAoIdGeradoNoHeader()
    {
        // Arrange
        var cliente = _fixture.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/teste-erro/servidor");

        // Assert
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        var referencia = corpo.GetProperty("referencia").GetString();
        Assert.False(string.IsNullOrWhiteSpace(referencia));
        Assert.Equal(referencia, resposta.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task ErroDoUsuario_NaoTrazReferencia_MasAindaDevolveOHeader()
    {
        // Arrange
        var cliente = _fixture.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/teste-erro/nao-encontrado");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Registro de teste não encontrado.", corpo.GetProperty("error").GetString());
        // O usuário tem o que corrigir: um código ao lado da frase só acrescentaria ruído.
        Assert.False(corpo.TryGetProperty("referencia", out _));
        // O header, esse, acompanha toda resposta — inclusive a de erro (CORS já o expõe para isso).
        Assert.True(resposta.Headers.Contains(Header));
    }
}
