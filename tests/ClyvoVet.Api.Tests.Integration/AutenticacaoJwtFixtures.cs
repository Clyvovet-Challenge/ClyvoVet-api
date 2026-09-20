using ClyvoVet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A API inteira em memória com o segredo de teste e o <see cref="ControllerDeAutenticacaoDeTeste"/>.
/// As variantes mudam só a configuração — sem duplicar a suíte.
/// </summary>
public class AutenticacaoJwtFixture : WebApplicationFactory<Program>
{
    protected virtual string? Segredo => TokensDeTeste.Segredo;
    protected virtual bool? ExigirToken => null;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            var valores = new Dictionary<string, string?> { ["Jwt:Secret"] = Segredo };
            if (ExigirToken is not null)
                valores["Auth:ExigirToken"] = ExigirToken.Value ? "true" : "false";
            configuracao.AddInMemoryCollection(valores);
        });

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"AutenticacaoJwtDb-{Guid.NewGuid()}"));

            // Os controllers deste assembly de teste entram no pipeline da API.
            services.AddControllers().AddApplicationPart(typeof(ControllerDeAutenticacaoDeTeste).Assembly);
        });
    }
}

/// <summary>Sem <c>Jwt:Secret</c>: a API tem de subir e responder 401, não cair.</summary>
public class SemSegredoFixture : AutenticacaoJwtFixture
{
    protected override string? Segredo => null;
}

/// <summary>A alavanca <c>Auth:ExigirToken=false</c>: as políticas passam a autorizar sem token.</summary>
public class ExigirTokenDesligadoFixture : AutenticacaoJwtFixture
{
    protected override bool? ExigirToken => false;
}
