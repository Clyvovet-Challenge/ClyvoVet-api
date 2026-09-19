using System.Net;

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
}
