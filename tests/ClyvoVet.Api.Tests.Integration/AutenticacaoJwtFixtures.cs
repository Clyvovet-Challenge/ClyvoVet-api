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

/// <summary>
/// O segredo que o deploy da Java manda gerar (<c>openssl rand -base64 48</c>): 48 bytes, com os quais a
/// Java assina em HS384.
/// </summary>
public class Segredo48BytesFixture : AutenticacaoJwtFixture
{
    public static readonly byte[] Chave = Enumerable.Range(1, 48).Select(i => (byte)i).ToArray();

    protected override string? Segredo => Convert.ToBase64String(Chave);
}

/// <summary>Um segredo de 64 bytes, com o qual a Java assina em HS512.</summary>
public class Segredo64BytesFixture : AutenticacaoJwtFixture
{
    public static readonly byte[] Chave = Enumerable.Range(1, 64).Select(i => (byte)i).ToArray();

    protected override string? Segredo => Convert.ToBase64String(Chave);
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
