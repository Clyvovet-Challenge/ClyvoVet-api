using System.Net;
using System.Text.Json;
using Moq;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace ClyvoVet.Api.Tests.Integration;

public class HealthCompletoEndpointsTests : IClassFixture<HealthCompletoFixture>
{
    private readonly HealthCompletoFixture _fixture;

    public HealthCompletoEndpointsTests(HealthCompletoFixture fixture)
    {
        _fixture = fixture;
        _fixture.Telegram.Reset();
    }

    private static Dictionary<string, JsonElement> ChecksPorNome(JsonElement raiz) =>
        raiz.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.Clone());

    [Fact]
    public async Task GetHealth_DependenciasRespondendo_RetornaOkComCadaCheckDetalhado()
    {
        // Arrange
        _fixture.Telegram.Setup(c => c.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, IsBot = true, FirstName = "ClyvoVet", Username = "bot_de_teste" });
        var cliente = _fixture.Server.CreateClient(); // sem token: health é anônimo

        // Act
        var resposta = await cliente.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.GetProperty("totalDurationMs").GetDouble() >= 0);

        var checks = ChecksPorNome(json.RootElement);
        // Testing nunca usa Mongo; os três de sempre, cada um com status, duração e tags.
        Assert.Equal(new[] { "mysql-database", "self", "telegram-bot" }, checks.Keys.Order());
        foreach (var check in checks.Values)
        {
            Assert.Equal("Healthy", check.GetProperty("status").GetString());
            Assert.True(check.GetProperty("durationMs").GetDouble() >= 0);
            Assert.NotEmpty(check.GetProperty("tags").EnumerateArray());
        }
        Assert.Equal("Bot @bot_de_teste respondendo.", checks["telegram-bot"].GetProperty("description").GetString());
    }

    [Fact]
    public async Task GetHealth_TelegramForaDoAr_Retorna503MasReadyContinuaOk()
    {
        // Arrange
        _fixture.Telegram.Setup(c => c.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("sem rede"));
        var cliente = _fixture.Server.CreateClient();

        // Act
        var geral = await cliente.GetAsync("/health");
        var ready = await cliente.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, geral.StatusCode);
        using var json = JsonDocument.Parse(await geral.Content.ReadAsStringAsync());
        Assert.Equal("Unhealthy", json.RootElement.GetProperty("status").GetString());
        var telegram = ChecksPorNome(json.RootElement)["telegram-bot"];
        Assert.Equal("Unhealthy", telegram.GetProperty("status").GetString());
        Assert.Equal("sem rede", telegram.GetProperty("error").GetString());

        // Telegram fica fora de "ready" de propósito: a instabilidade dele não tira a API de rotação.
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }
}
