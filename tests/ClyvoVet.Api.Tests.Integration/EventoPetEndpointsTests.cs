using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class EventoPetEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public EventoPetEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_SemApiKey_RetornaUnauthorized()
    {
        // Arrange
        var clientSemApiKey = _fixture.Server.CreateClient();

        // Act
        var response = await clientSemApiKey.GetAsync("/api/v1/eventos-pet");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ApiKeyErrada_RetornaUnauthorized()
    {
        // Arrange
        var clientComApiKeyErrada = _fixture.Server.CreateClient();
        clientComApiKeyErrada.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");

        // Act
        var response = await clientComApiKeyErrada.GetAsync("/api/v1/eventos-pet");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_SemFiltros_RetornaOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/eventos-pet");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_DadosValidos_RetornaCreated()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Feira de Adoção",
            Tipo = TipoEventoPetEnum.Feira,
            Cidade = "São Paulo",
            Estado = "SP",
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(11)),
            EspecieAlvo = EspecieEnum.Todos,
            Gratuito = true,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/eventos-pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventoPetResponse>();
        Assert.NotNull(created);
        Assert.Equal(request.Titulo, created!.Titulo);
    }

    [Fact]
    public async Task Create_DataInicioNoPassado_RetornaBadRequest()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Evento Passado",
            Tipo = TipoEventoPetEnum.Vacinacao,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            EspecieAlvo = EspecieEnum.Todos,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/eventos-pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("passado", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_DataFimAnteriorADataInicio_RetornaBadRequest()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Datas Invertidas",
            Tipo = TipoEventoPetEnum.Workshop,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EspecieAlvo = EspecieEnum.Todos,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/eventos-pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_RetornaNotFound()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/eventos-pet/id-que-nao-existe");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltroPorCidade_RetornaApenasEventosDaCidade()
    {
        // Arrange
        var eventoCuritiba = new EventoPetRequest
        {
            Titulo = "Vacinação Curitiba",
            Tipo = TipoEventoPetEnum.Vacinacao,
            Cidade = "Curitiba",
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            EspecieAlvo = EspecieEnum.Todos,
            Ativo = true
        };
        var eventoRecife = new EventoPetRequest
        {
            Titulo = "Feira Recife",
            Tipo = TipoEventoPetEnum.Feira,
            Cidade = "Recife",
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            EspecieAlvo = EspecieEnum.Todos,
            Ativo = true
        };
        await _client.PostAsJsonAsync("/api/v1/eventos-pet", eventoCuritiba);
        await _client.PostAsJsonAsync("/api/v1/eventos-pet", eventoRecife);

        // Act
        var response = await _client.GetAsync("/api/v1/eventos-pet?cidade=Curitiba");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<EventoPetResponse>>();
        Assert.NotNull(result);
        Assert.All(result!, item => Assert.Equal("Curitiba", item.Cidade));
    }

    [Fact]
    public async Task GetAll_FiltroPorTipo_RetornaApenasEventosDoTipo()
    {
        // Arrange
        var evento = new EventoPetRequest
        {
            Titulo = "Castração Comunitária",
            Tipo = TipoEventoPetEnum.Castracao,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            EspecieAlvo = EspecieEnum.Todos,
            Ativo = true
        };
        await _client.PostAsJsonAsync("/api/v1/eventos-pet", evento);

        // Act
        var response = await _client.GetAsync("/api/v1/eventos-pet?tipo=Castracao");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<EventoPetResponse>>();
        Assert.NotNull(result);
        Assert.All(result!, item => Assert.Equal(TipoEventoPetEnum.Castracao, item.Tipo));
    }

    [Fact]
    public async Task GetAll_FiltroPorEspecieAlvo_RetornaApenasEventosDaEspecie()
    {
        // Arrange
        var evento = new EventoPetRequest
        {
            Titulo = "Feira Equina",
            Tipo = TipoEventoPetEnum.Feira,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            EspecieAlvo = EspecieEnum.Equino,
            Ativo = true
        };
        await _client.PostAsJsonAsync("/api/v1/eventos-pet", evento);

        // Act
        var response = await _client.GetAsync("/api/v1/eventos-pet?especieAlvo=Equino");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<EventoPetResponse>>();
        Assert.NotNull(result);
        Assert.All(result!, item => Assert.Equal(EspecieEnum.Equino, item.EspecieAlvo));
    }
}
