using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

// Substitui o ITelegramService real por um fake — sem isso, o teste bateria de
// verdade no Telegram (rede externa, token real, resultado não determinístico).
public class FakeTelegramService : ITelegramService
{
    public long? UltimoChatId { get; private set; }
    public string? UltimaMensagem { get; private set; }

    public Task EnviarMensagemAsync(long chatId, string mensagem)
    {
        UltimoChatId = chatId;
        UltimaMensagem = mensagem;
        return Task.CompletedTask;
    }
}

// Simula uma falha do Telegram (ex.: bot bloqueado pelo usuário, chatId inválido)
// para confirmar que o middleware global trata o erro em vez de vazar um 500.
public class FakeTelegramServiceComFalha : ITelegramService
{
    public Task EnviarMensagemAsync(long chatId, string mensagem)
    {
        throw new BadRequestException("Falha simulada do Telegram");
    }
}

public class TelegramTestFixture : WebApplicationFactory<Program>
{
    public readonly FakeTelegramService FakeService = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"ClyvoVetTestDb-{Guid.NewGuid()}"));

            var telegramDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITelegramService));
            if (telegramDescriptor is not null)
                services.Remove(telegramDescriptor);

            services.AddSingleton<ITelegramService>(FakeService);
        });
    }
}

public class TelegramFalhaTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"ClyvoVetTestDb-{Guid.NewGuid()}"));

            var telegramDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITelegramService));
            if (telegramDescriptor is not null)
                services.Remove(telegramDescriptor);

            services.AddSingleton<ITelegramService, FakeTelegramServiceComFalha>();
        });
    }
}

public class TelegramEndpointsTests
{
    [Fact]
    public async Task Enviar_DadosValidos_RetornaNoContentEChamaServicoComOsDadosCorretos()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_TELEGRAM_API_KEY");
        var request = new TelegramRequest { ChatId = 123456789, Mensagem = "Teste ClyvoVet" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(123456789, factory.FakeService.UltimoChatId);
        Assert.Equal("Teste ClyvoVet", factory.FakeService.UltimaMensagem);
    }

    [Fact]
    public async Task Enviar_MensagemVazia_RetornaBadRequest()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_TELEGRAM_API_KEY");
        var request = new TelegramRequest { ChatId = 123456789, Mensagem = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enviar_ServicoLancaExcecao_RetornaBadRequest()
    {
        // Arrange
        using var factory = new TelegramFalhaTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_TELEGRAM_API_KEY");
        var request = new TelegramRequest { ChatId = 123456789, Mensagem = "Teste" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enviar_SemApiKey_RetornaUnauthorized()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        var request = new TelegramRequest { ChatId = 123456789, Mensagem = "Teste" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enviar_ApiKeyErrada_RetornaUnauthorized()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");
        var request = new TelegramRequest { ChatId = 123456789, Mensagem = "Teste" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GerarLink_TutorIdValido_RetornaLinkComTutorId()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_TELEGRAM_API_KEY");

        // Act
        var response = await client.GetAsync("/api/v1/telegram/link/tutor-abc-123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TelegramLinkResponse>();
        Assert.NotNull(result);
        Assert.Contains("start=tutor-abc-123", result!.Link);
        Assert.StartsWith("https://t.me/", result.Link);
    }

    [Fact]
    public async Task GerarLink_SemApiKey_RetornaUnauthorized()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/telegram/link/tutor-abc-123");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
