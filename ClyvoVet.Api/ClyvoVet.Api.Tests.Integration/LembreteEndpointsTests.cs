using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class LembreteEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public LembreteEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Create_DadosValidos_RetornaCreatedComStatusPendente()
    {
        // Arrange
        var request = new LembreteRequest
        {
            AnimalId = _fixture.AnimalId,
            Titulo = "Vacina Antirrábica",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(15),
            Recorrente = false,
            Status = StatusLembreteEnum.Enviado // deve ser ignorado e forçado a Pendente
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/lembretes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LembreteResponse>();
        Assert.NotNull(created);
        Assert.Equal(StatusLembreteEnum.Pendente, created!.Status);
        Assert.Equal("Rex", created.NomeAnimal);
    }

    [Fact]
    public async Task Create_AnimalIdInexistente_RetornaNotFound()
    {
        // Arrange
        var request = new LembreteRequest
        {
            AnimalId = "00000000-0000-0000-0000-000000000000",
            Titulo = "Lembrete Sem Animal",
            Tipo = TipoLembreteEnum.Consulta,
            AgendadoEm = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/lembretes", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DataAgendadaNoPassado_RetornaBadRequest()
    {
        // Arrange
        var request = new LembreteRequest
        {
            AnimalId = _fixture.AnimalId,
            Titulo = "Lembrete Data Passada",
            Tipo = TipoLembreteEnum.Medicamento,
            AgendadoEm = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/lembretes", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltrandoPorAnimalId_RetornaOk()
    {
        // Arrange
        var url = $"/api/v1/lembretes?animalId={_fixture.AnimalId}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
