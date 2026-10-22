using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Telegram.Bot;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A API da suíte com a Bot API do Telegram trocada por um dublê. O /health agregado executa o check
/// do Telegram, e com o cliente real ele chamaria a Bot API de verdade (já prendeu a suíte por ~100 s).
/// Cada teste arma o dublê que precisa (a classe de testes roda em sequência).
/// </summary>
public class HealthCompletoFixture : IntegrationTestFixture
{
    public Mock<ITelegramBotClient> Telegram { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITelegramBotClient>();
            services.AddSingleton(Telegram.Object);
        });
    }
}
