using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

// Substitui o IWhatsAppService real por um fake — sem isso, o teste bateria de
// verdade no Twilio (rede externa, credenciais reais, resultado não determinístico).
public class FakeWhatsAppService : IWhatsAppService
{
    public string? UltimoTelefone { get; private set; }
    public string? UltimaMensagem { get; private set; }

    public Task EnviarMensagemAsync(string telefone, string mensagem)
    {
        UltimoTelefone = telefone;
        UltimaMensagem = mensagem;
        return Task.CompletedTask;
    }
}

// Simula uma falha do Twilio (ex.: número fora do sandbox, credenciais inválidas)
// para confirmar que o middleware global trata o erro em vez de vazar um 500.
public class FakeWhatsAppServiceComFalha : IWhatsAppService
{
    public Task EnviarMensagemAsync(string telefone, string mensagem)
    {
        throw new BadRequestException("Falha simulada do Twilio");
    }
}

public class WhatsAppTestFixture : WebApplicationFactory<Program>
{
    public readonly FakeWhatsAppService FakeService = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"ClyvoVetTestDb-{Guid.NewGuid()}"));

            var whatsAppDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWhatsAppService));
            if (whatsAppDescriptor is not null)
                services.Remove(whatsAppDescriptor);

            services.AddSingleton<IWhatsAppService>(FakeService);
        });
    }
}

public class WhatsAppFalhaTestFixture : WebApplicationFactory<Program>
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

            var whatsAppDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWhatsAppService));
            if (whatsAppDescriptor is not null)
                services.Remove(whatsAppDescriptor);

            services.AddSingleton<IWhatsAppService, FakeWhatsAppServiceComFalha>();
        });
    }
}

public class WhatsAppEndpointsTests
{
    [Fact]
    public async Task Enviar_DadosValidos_RetornaNoContentEChamaServicoComOsDadosCorretos()
    {
        // Arrange
        using var factory = new WhatsAppTestFixture();
        var client = factory.CreateClient();
        var request = new WhatsAppRequest { Telefone = "+5511999999999", Mensagem = "Teste ClyvoVet" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/whatsapp/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("+5511999999999", factory.FakeService.UltimoTelefone);
        Assert.Equal("Teste ClyvoVet", factory.FakeService.UltimaMensagem);
    }

    [Fact]
    public async Task Enviar_TelefoneVazio_RetornaBadRequest()
    {
        // Arrange
        using var factory = new WhatsAppTestFixture();
        var client = factory.CreateClient();
        var request = new WhatsAppRequest { Telefone = "", Mensagem = "Teste" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/whatsapp/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enviar_ServicoLancaExcecao_RetornaBadRequest()
    {
        // Arrange
        using var factory = new WhatsAppFalhaTestFixture();
        var client = factory.CreateClient();
        var request = new WhatsAppRequest { Telefone = "+5511999999999", Mensagem = "Teste" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/whatsapp/enviar", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
