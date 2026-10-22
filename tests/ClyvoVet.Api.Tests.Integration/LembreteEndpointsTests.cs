using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;

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
    public async Task GetAll_SemApiKey_RetornaUnauthorized()
    {
        // Arrange
        var clientSemApiKey = _fixture.CreateClientComBearer();

        // Act
        var response = await clientSemApiKey.GetAsync("/api/v1/lembretes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ApiKeyErrada_RetornaUnauthorized()
    {
        // Arrange
        var clientComApiKeyErrada = _fixture.CreateClientComBearer();
        clientComApiKeyErrada.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");

        // Act
        var response = await clientComApiKeyErrada.GetAsync("/api/v1/lembretes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    /// <summary>
    /// O app manda a hora de Brasília sem fuso ("2026-10-01T10:00:00"). Comparada com o
    /// relógio UTC, três horas à frente, um lembrete para daqui a uma hora era recusado como
    /// passado. Brasília = UTC−3 (sem horário de verão desde 2019).
    /// </summary>
    [Fact]
    public async Task Create_DaquiAUmaHoraEmBrasiliaSemFuso_RetornaCreated()
    {
        // Arrange
        var daquiAUmaHoraEmBrasilia = DateTime.SpecifyKind(
            DateTime.UtcNow.AddHours(-3).AddHours(1), DateTimeKind.Unspecified);
        var request = new LembreteRequest
        {
            AnimalId = _fixture.AnimalId,
            Titulo = "Vermífugo daqui a uma hora",
            Tipo = TipoLembreteEnum.Medicamento,
            AgendadoEm = daquiAUmaHoraEmBrasilia
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/lembretes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
