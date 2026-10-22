using ClyvoVet.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace ClyvoVet.Api.Tests.Unit;

public class TelegramHealthCheckTests
{
    // GetMe é método de extensão; por baixo ele chama SendRequest(GetMeRequest), que é da interface.
    private static Mock<ITelegramBotClient> Bot() => new();

    [Fact]
    public async Task CheckHealthAsync_BotRespondendo_RetornaHealthyComONomeDoBot()
    {
        // Arrange
        var bot = Bot();
        bot.Setup(c => c.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, IsBot = true, FirstName = "ClyvoVet", Username = "bot_de_teste" });

        // Act
        var resultado = await new TelegramHealthCheck(bot.Object).CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
        Assert.Equal("Bot @bot_de_teste respondendo.", resultado.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_BotApiInacessivel_RetornaUnhealthyComAExcecao()
    {
        // Arrange
        var bot = Bot();
        bot.Setup(c => c.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("sem rede"));

        // Act
        var resultado = await new TelegramHealthCheck(bot.Object).CheckHealthAsync(new HealthCheckContext());

        // Assert
        // Unhealthy, e não exceção: quem decide o 503 é o endpoint, e só no /health agregado.
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.IsType<HttpRequestException>(resultado.Exception);
    }
}
