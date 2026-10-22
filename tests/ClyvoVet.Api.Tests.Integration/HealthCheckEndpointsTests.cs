using System.Net;
using System.Text.Json;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class HealthCheckEndpointsTests
{
    private readonly HttpClient _client;

    public HealthCheckEndpointsTests(IntegrationTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetLive_ApiEmExecucao_RetornaOkComStatusHealthy()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\": \"Healthy\"", body);
    }

    [Fact]
    public async Task GetReady_BancoInMemoryDisponivel_RetornaOkComStatusHealthy()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\": \"Healthy\"", body);
    }

    [Fact]
    public async Task GetLive_SoOCheckDoProcesso_NaoConsultaDependencias()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var nomes = json.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString());
        // Liveness que consultasse o banco reiniciaria o contêiner por culpa do banco.
        Assert.Equal(new[] { "self" }, nomes);
    }

    [Fact]
    public async Task GetReady_SoOBanco_TelegramFicaDeFora()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var nomes = json.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString());
        Assert.Equal(new[] { "mysql-database" }, nomes);
    }
}
