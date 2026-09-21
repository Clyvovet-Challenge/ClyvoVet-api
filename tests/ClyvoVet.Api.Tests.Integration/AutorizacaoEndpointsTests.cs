using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A autorização nas rotas REAIS. O cliente é "cru" (<c>Server.CreateClient()</c>, sem os headers
/// padrão da fixture) e cada teste manda só o que quer provar — a <c>X-Api-Key</c> certa, quando o
/// 401 esperado tem de vir do JWT, e não do filtro da chave.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AutorizacaoEndpointsTests
{
    private readonly IntegrationTestFixture _fixture;

    public AutorizacaoEndpointsTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    [InlineData("/api/v1/sugestoes-produto")]
    [InlineData("/api/v1/produtos")]
    [InlineData("/api/v1/saude-preditiva/qualquer-id")]
    [InlineData("/api/v1/widget-saude-preditiva/qualquer-id")]
    public async Task RotaProtegida_SemToken_RetornaUnauthorized(string rota)
    {
        // Arrange
        var cliente = _fixture.Server.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/produtos")]
    [InlineData("PUT", "/api/v1/produtos/qualquer-id")]
    [InlineData("DELETE", "/api/v1/produtos/qualquer-id")]
    public async Task Produto_EscritaComPerfilTutor_RetornaForbidden(string metodo, string rota)
    {
        // Arrange
        var cliente = _fixture.CreateClientComBearer("TUTOR");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), rota) { Content = JsonContent.Create(new { }) };

        // Act
        var resposta = await cliente.SendAsync(requisicao);

        // Assert
        // A autorização roda antes do model binding: 403 mesmo com corpo vazio.
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Produto_LeituraComPerfilTutor_RetornaOk()
    {
        // Arrange
        var cliente = _fixture.CreateClientComBearer("TUTOR");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var resposta = await cliente.GetAsync("/api/v1/produtos");

        // Assert
        // A vitrine do tutor é leitura: só escrever exige a equipe.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task RotasDeInfraestrutura_SemToken_ContinuamRespondendo(string rota)
    {
        // Arrange
        var cliente = _fixture.Server.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        // Sem FallbackPolicy: um health check quebrado tira a aplicação de rotação no Render.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public void Health_Completo_NaoTemAutorizacaoNoEndpoint()
    {
        // Arrange
        var endpoints = _fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        // Act
        var health = endpoints.Single(e => e.RoutePattern.RawText == "/health");

        // Assert
        // Não executa o check: o do Telegram chama a Bot API de verdade (A8), e com a rede lenta uma
        // chamada aqui prendia a suíte por até ~100 s. Que a rota é anônima é o que importa; que
        // nenhuma política global a derruba já é provado por /health/live e /health/ready
        // respondendo 200 sem token.
        Assert.Empty(health.Metadata.OfType<IAuthorizeData>());
    }
}

/// <summary>Alavanca desligada, escopo ligado: as duas travas independentes e como elas se compõem.</summary>
public class AlavancaDesligadaFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TokensDeTeste.Segredo,
                ["Auth:ExigirToken"] = "false",
                ["Api:EscopoPorTutor"] = "true",
            }));

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"AlavancaDb-{Guid.NewGuid()}"));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
    }
}

public class AlavancaDesligadaTests : IClassFixture<AlavancaDesligadaFixture>
{
    private readonly AlavancaDesligadaFixture _fixture;

    public AlavancaDesligadaTests(AlavancaDesligadaFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExigirTokenDesligado_RotaSemEscopo_ResponderOkSemToken()
    {
        var resposta = await _fixture.CreateClient().GetAsync("/api/v1/produtos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task ExigirTokenDesligado_ComEscopoLigadoESemToken_RetornaForbidden()
    {
        // A alavanca abre a AUTENTICAÇÃO, não o escopo: sem tutor no token, quem recorta por dono
        // continua negando (o 403 que o EscopoPorTutorEndpointsTests provava antes da F2, A7).
        var resposta = await _fixture.CreateClient().GetAsync("/api/v1/lembretes");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
