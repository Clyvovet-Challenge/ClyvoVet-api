using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class SugestaoProdutoEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public SugestaoProdutoEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Create_DadosValidos_RetornaCreatedComNomesPreenchidos()
    {
        // Arrange
        var request = new SugestaoProdutoRequest
        {
            AnimalId = _fixture.AnimalId,
            ProdutoId = _fixture.ProdutoId,
            Justificativa = "Recomendado pelo veterinário.",
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sugestoes-produto", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SugestaoProdutoResponse>();
        Assert.NotNull(created);
        Assert.Equal("Rex", created!.NomeAnimal);
        Assert.Equal("Ração de Teste", created.NomeProduto);
    }

    [Fact]
    public async Task Create_ProdutoIdInexistente_RetornaNotFound()
    {
        // Arrange
        var request = new SugestaoProdutoRequest
        {
            AnimalId = _fixture.AnimalId,
            ProdutoId = "00000000-0000-0000-0000-000000000000",
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sugestoes-produto", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_AnimalIdInexistente_RetornaNotFound()
    {
        // Arrange
        var request = new SugestaoProdutoRequest
        {
            AnimalId = "00000000-0000-0000-0000-000000000000",
            ProdutoId = _fixture.ProdutoId,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sugestoes-produto", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltrandoPorAnimalId_RetornaOk()
    {
        // Arrange
        var url = $"/api/v1/sugestoes-produto?animalId={_fixture.AnimalId}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_RetornaNotFound()
    {
        // Arrange
        const string idInexistente = "id-que-nao-existe";

        // Act
        var response = await _client.GetAsync($"/api/v1/sugestoes-produto/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateLerAtualizarEDeletar_FluxoCompleto_RetornaStatusEsperadosEmCadaEtapa()
    {
        // Arrange
        var request = new SugestaoProdutoRequest
        {
            AnimalId = _fixture.AnimalId,
            ProdutoId = _fixture.ProdutoId,
            Justificativa = "Justificativa original.",
            Ativo = true
        };

        // Act — cria
        var createResponse = await _client.PostAsJsonAsync("/api/v1/sugestoes-produto", request);
        var created = await createResponse.Content.ReadFromJsonAsync<SugestaoProdutoResponse>();

        // Act — busca
        var getResponse = await _client.GetAsync($"/api/v1/sugestoes-produto/{created!.Id}");

        // Act — atualiza
        request.Justificativa = "Justificativa atualizada após reavaliação.";
        request.Ativo = false;
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/sugestoes-produto/{created.Id}", request);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SugestaoProdutoResponse>();

        // Act — deleta
        var deleteResponse = await _client.DeleteAsync($"/api/v1/sugestoes-produto/{created.Id}");

        // Act — confirma remoção
        var getAfterDeleteResponse = await _client.GetAsync($"/api/v1/sugestoes-produto/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Justificativa atualizada após reavaliação.", updated!.Justificativa);
        Assert.False(updated.Ativo);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }
}
