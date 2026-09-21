using System.Net;
using System.Text.Json;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class TratamentoDeErrosEndpointsTests
{
    private readonly HttpClient _client;

    public TratamentoDeErrosEndpointsTests(IntegrationTestFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task GetById_ProdutoInexistente_RetornaNotFoundComErrorEProblemDetails()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync($"/api/v1/produtos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Contains("json", resposta.Content.Headers.ContentType!.MediaType);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
        Assert.Equal(404, corpo.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task GetAll_PaginaInvalida_RetornaBadRequestComError()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/v1/produtos?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
    }
}
