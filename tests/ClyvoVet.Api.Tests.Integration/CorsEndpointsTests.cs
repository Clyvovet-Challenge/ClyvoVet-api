namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class CorsEndpointsTests
{
    private readonly HttpClient _client;

    public CorsEndpointsTests(IntegrationTestFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task Cors_OrigemPermitida_ExpoeOsCabecalhosDeListagem()
    {
        // Arrange: sem expor X-Total-Count e Link, o navegador (Expo web) não deixa o JS lê-los.
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        requisicao.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var resposta = await _client.SendAsync(requisicao);

        // Assert
        var expostos = string.Join(",", resposta.Headers.GetValues("Access-Control-Expose-Headers"));
        Assert.Contains("X-Total-Count", expostos);
        Assert.Contains("Link", expostos);
        Assert.Contains("X-Correlation-Id", expostos);
    }
}
