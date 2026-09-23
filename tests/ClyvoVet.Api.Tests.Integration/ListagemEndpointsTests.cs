using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// O contrato das listagens pelo pipeline real: corpo array (o que o app lê), total e navegação nos
/// cabeçalhos, e 400 com <c>error</c> para pedido inválido.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ListagemEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public ListagemEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private static async Task<string> ErroAsync(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetString()!;

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    public async Task GetAll_SemParametrosNovos_DevolveArrayComTotalELink(string rota)
    {
        // Act
        var resposta = await _client.GetAsync(rota);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.ValueKind);
        Assert.True(int.Parse(resposta.Headers.GetValues("X-Total-Count").Single()) >= 0);
        var link = resposta.Headers.GetValues("Link").Single();
        Assert.Contains("rel=\"first\"", link);
        Assert.Contains("rel=\"last\"", link);
    }

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    public async Task GetAll_OrdenarPorCampoInvalido_RetornaBadRequestComErrorEOsPermitidos(string rota)
    {
        var resposta = await _client.GetAsync($"{rota}?ordenarPor=senha");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.StartsWith("Campo de ordenação 'senha' inválido. Permitidos: ", await ErroAsync(resposta));
    }

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    public async Task GetAll_DirecaoInvalidaComOrdenarPor_RetornaBadRequest(string rota)
    {
        var resposta = await _client.GetAsync($"{rota}?ordenarPor=criadoEm&direcao=sideways");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("Direção 'sideways' inválida. Use 'asc' ou 'desc'.", await ErroAsync(resposta));
    }

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    public async Task GetAll_DirecaoSemOrdenarPor_EhIgnorada(string rota)
    {
        var resposta = await _client.GetAsync($"{rota}?direcao=sideways");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    public async Task GetAll_PaginaAlemDoFim_RetornaOkComListaVaziaETotal(string rota)
    {
        var resposta = await _client.GetAsync($"{rota}?page=100000&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("[]", (await resposta.Content.ReadAsStringAsync()).Trim());
        Assert.True(resposta.Headers.Contains("X-Total-Count"));
    }

    [Fact]
    public async Task GetAll_LembretesOrdenadosPorTituloDesc_VemDoMaiorParaOMenor()
    {
        // Arrange: prefixo único, porque a fixture é compartilhada e outros testes também criam lembretes.
        var prefixo = $"ord-{Guid.NewGuid():N}";
        foreach (var sufixo in new[] { "Charlie", "Alpha", "Bravo" })
        {
            var criado = await _client.PostAsJsonAsync("/api/v1/lembretes", new LembreteRequest
            {
                AnimalId = _fixture.AnimalId,
                Titulo = $"{prefixo} {sufixo}",
                Tipo = TipoLembreteEnum.Consulta,
                AgendadoEm = DateTime.UtcNow.AddDays(10),
            });
            Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        }

        // Act
        var lista = await _client.GetFromJsonAsync<List<LembreteResponse>>(
            $"/api/v1/lembretes?animalId={_fixture.AnimalId}&pageSize=100&ordenarPor=titulo&direcao=desc");

        // Assert
        var meus = lista!.Where(l => l.Titulo.StartsWith(prefixo)).Select(l => l.Titulo[(prefixo.Length + 1)..]).ToList();
        Assert.Equal(["Charlie", "Bravo", "Alpha"], meus);
    }
}
