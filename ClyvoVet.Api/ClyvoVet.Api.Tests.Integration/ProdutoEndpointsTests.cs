using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ProdutoEndpointsTests
{
    private readonly HttpClient _client;

    public ProdutoEndpointsTests(IntegrationTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_ComProdutoSeedCadastrado_RetornaOkComLista()
    {
        // Arrange
        // (produto de seed já criado pela fixture)

        // Act
        var response = await _client.GetAsync("/api/v1/produtos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var produtos = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();
        Assert.NotNull(produtos);
        Assert.NotEmpty(produtos);
    }

    [Fact]
    public async Task GetAll_PageInvalido_RetornaBadRequest()
    {
        // Arrange
        const string url = "/api/v1/produtos?page=0";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_PageSizeLimitado_RespeitaOTamanhoDaPagina()
    {
        // Arrange
        for (var i = 0; i < 3; i++)
        {
            var request = new ProdutoRequest
            {
                Nome = $"Produto Paginacao {Guid.NewGuid()}",
                Categoria = CategoriaEnum.Acessorio,
                Preco = 10m,
                EspecieIndicada = EspecieEnum.Todos,
                Ativo = true
            };
            await _client.PostAsJsonAsync("/api/v1/produtos", request);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/produtos?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();
        Assert.NotNull(result);
        Assert.True(result!.Count <= 2);
    }

    [Fact]
    public async Task Create_DadosValidos_RetornaCreatedComProduto()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Shampoo Pet Neutro 500ml",
            Descricao = "Shampoo hipoalergênico.",
            Categoria = CategoriaEnum.Acessorio,
            Preco = 28.90m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.Id));
        Assert.Equal(request.Nome, created.Nome);
    }

    [Fact]
    public async Task Create_PrecoNegativo_RetornaBadRequest()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Produto Inválido",
            Categoria = CategoriaEnum.Racao,
            Preco = -50m,
            EspecieIndicada = EspecieEnum.Cachorro,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_RetornaNotFound()
    {
        // Arrange
        const string idInexistente = "id-que-nao-existe";

        // Act
        var response = await _client.GetAsync($"/api/v1/produtos/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateELerEDeletar_FluxoCompleto_RetornaStatusEsperadosEmCadaEtapa()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Produto Para Deletar",
            Categoria = CategoriaEnum.Outro,
            Preco = 10m,
            EspecieIndicada = EspecieEnum.Outro,
            Ativo = true
        };

        // Act — cria
        var createResponse = await _client.PostAsJsonAsync("/api/v1/produtos", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ProdutoResponse>();

        // Act — busca
        var getResponse = await _client.GetAsync($"/api/v1/produtos/{created!.Id}");

        // Act — atualiza
        request.Nome = "Produto Atualizado";
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/produtos/{created.Id}", request);

        // Act — deleta
        var deleteResponse = await _client.DeleteAsync($"/api/v1/produtos/{created.Id}");

        // Act — confirma remoção
        var getAfterDeleteResponse = await _client.GetAsync($"/api/v1/produtos/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }
}
