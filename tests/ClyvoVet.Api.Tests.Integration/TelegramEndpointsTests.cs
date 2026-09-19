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

    /// <summary>
    /// Este teste afirmava o contrario: que o link CONTINHA o tutorId. Ele passava,
    /// e o que ele protegia era o buraco -- o bot e publico, e enquanto o parametro
    /// era o id do tutor, qualquer pessoa digitava /start com o id alheio e assumia
    /// as notificacoes daquele tutor. Agora ele afirma que o id NAO aparece.
    /// </summary>
    [Fact]
    public async Task GerarLink_NaoVazaOTutorIdNaUrl()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var response = await client.GetAsync("/api/v1/telegram/link/tutor-abc-123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TelegramLinkResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("https://t.me/", result!.Link);
        Assert.DoesNotContain("tutor-abc-123", result.Link);
        Assert.Contains("?start=", result.Link);
    }

    /// <summary>
    /// O parametro start do Telegram aceita somente A-Z a-z 0-9 _ - e no maximo 64
    /// caracteres. Um token fora disso faz o Telegram recusar o link inteiro no
    /// cliente, onde nenhum log desta API enxergaria.
    /// </summary>
    [Fact]
    public async Task GerarLink_TokenCabeNoAlfabetoQueOTelegramAceita()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var response = await client.GetAsync("/api/v1/telegram/link/tutor-abc-123");
        var result = await response.Content.ReadFromJsonAsync<TelegramLinkResponse>();

        // Assert
        var token = result!.Link.Split("?start=")[1];
        Assert.InRange(token.Length, 1, 64);
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
    }

    /// <summary>Dois pedidos nunca devolvem o mesmo convite.</summary>
    [Fact]
    public async Task GerarLink_DoisPedidos_DevolvemTokensDiferentes()
    {
        // Arrange
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var primeiro = await (await client.GetAsync("/api/v1/telegram/link/tutor-abc-123"))
            .Content.ReadFromJsonAsync<TelegramLinkResponse>();
        var segundo = await (await client.GetAsync("/api/v1/telegram/link/tutor-abc-123"))
            .Content.ReadFromJsonAsync<TelegramLinkResponse>();

        // Assert
        Assert.NotEqual(primeiro!.Link, segundo!.Link);
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
    // ─────────────────────────────────────────────────────────────────────────
    // A separacao das duas chaves
    //
    // E a razao de o filtro ter saido da classe. Se um dia alguem devolver o
    // TypeFilter para o topo do controller, um destes dois testes cai -- e o
    // motivo fica escrito aqui, em vez de virar uma decisao perdida no diff.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A chave do app abre o vinculo, que e do proprio tutor.
    /// </summary>
    [Fact]
    public async Task Vinculo_ComAChaveDoApp_Responde200()
    {
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        var response = await client.GetAsync("/api/v1/telegram/vinculo/tutor-abc-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// E NAO abre o envio. Este e o teste que justifica a mudanca: a chave que
    /// viaja dentro do aplicativo nao pode escrever para chat nenhum, senao quem
    /// extrair o bundle passa a falar como se fosse a clinica.
    /// </summary>
    [Fact]
    public async Task Enviar_ComAChaveDoApp_ContinuaRecusando()
    {
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        var response = await client.PostAsJsonAsync(
            "/api/v1/telegram/enviar",
            new TelegramRequest { ChatId = 123456789, Mensagem = "oi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.FakeService.UltimaMensagem);
    }

    /// <summary>
    /// Tutor sem vinculo responde 200 com <c>vinculado: false</c>, e nao 404.
    ///
    /// Nao ter vinculo e o estado em que TODO tutor comeca; trata-lo como erro
    /// obrigaria o app a desenhar a tela normal a partir de uma excecao.
    /// </summary>
    [Fact]
    public async Task Vinculo_SemVinculo_Responde200ComVinculadoFalso()
    {
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        var response = await client.GetAsync("/api/v1/telegram/vinculo/tutor-sem-vinculo");
        var corpo = await response.Content.ReadFromJsonAsync<TelegramVinculoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(corpo);
        Assert.False(corpo!.Vinculado);
        Assert.Null(corpo.Desde);
    }

    /// <summary>
    /// Desvincular sem vinculo tambem da 204: o pedido e "que este tutor nao
    /// receba mais", e esse estado passa a valer nos dois casos.
    /// </summary>
    [Fact]
    public async Task Desvincular_SemVinculo_Responde204()
    {
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        var response = await client.DeleteAsync("/api/v1/telegram/vinculo/tutor-sem-vinculo");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Vinculo_SemApiKey_RetornaUnauthorized()
    {
        using var factory = new TelegramTestFixture();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/telegram/vinculo/tutor-abc-123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
