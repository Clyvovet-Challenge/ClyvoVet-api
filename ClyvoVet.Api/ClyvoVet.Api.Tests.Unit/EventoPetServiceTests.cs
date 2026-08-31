using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class EventoPetServiceTests
{
    private readonly Mock<IEventoPetRepository> _repositoryMock = new();
    private readonly EventoPetService _service;

    public EventoPetServiceTests()
    {
        _service = new EventoPetService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_RepositorioRetornaEventos_RetornaListaMapeada()
    {
        // Arrange
        var eventos = new List<EventoPet>
        {
            new()
            {
                Id = "1",
                Titulo = "Feira de Adoção",
                Tipo = TipoEventoPetEnum.Feira,
                DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                EspecieAlvo = EspecieEnum.Todos
            }
        };
        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, null, null, null))
            .ReturnsAsync(eventos);

        // Act
        var result = await _service.GetAllAsync(1, 10, null, null, null);

        // Assert
        var resultList = Assert.Single(result);
        Assert.Equal("Feira de Adoção", resultList.Titulo);
    }

    [Fact]
    public async Task GetByIdAsync_IdExistente_RetornaEventoMapeado()
    {
        // Arrange
        var evento = new EventoPet
        {
            Id = "1",
            Titulo = "Feira de Adoção",
            Tipo = TipoEventoPetEnum.Feira,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EspecieAlvo = EspecieEnum.Todos
        };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(evento);

        // Act
        var result = await _service.GetByIdAsync("1");

        // Assert
        Assert.Equal("Feira de Adoção", result.Titulo);
    }

    [Fact]
    public async Task CreateAsync_DataInicioNoPassado_LancaBadRequestException()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Evento Passado",
            Tipo = TipoEventoPetEnum.Vacinacao,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            EspecieAlvo = EspecieEnum.Todos
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(request));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<EventoPet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DataFimAnteriorADataInicio_LancaBadRequestException()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Evento Datas Invertidas",
            Tipo = TipoEventoPetEnum.Workshop,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EspecieAlvo = EspecieEnum.Todos
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaEventoCriado()
    {
        // Arrange
        var request = new EventoPetRequest
        {
            Titulo = "Feira de Adoção",
            Tipo = TipoEventoPetEnum.Feira,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EspecieAlvo = EspecieEnum.Todos
        };
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<EventoPet>()))
            .ReturnsAsync((EventoPet e) =>
            {
                e.Id = "novo-id";
                return e;
            });

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("novo-id", result.Id);
        Assert.Equal(request.Titulo, result.Titulo);
    }

    [Fact]
    public async Task UpdateAsync_DataInicioAlteradaParaPassado_LancaBadRequestException()
    {
        // Arrange
        var existing = new EventoPet
        {
            Id = "1",
            Titulo = "Evento Original",
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);

        var request = new EventoPetRequest
        {
            Titulo = "Evento Original",
            Tipo = TipoEventoPetEnum.Outro,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            EspecieAlvo = EspecieEnum.Todos
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync("1", request));
    }

    [Fact]
    public async Task UpdateAsync_DataInicioMantidaNoPassadoSemAlteracao_NaoLancaExcecao()
    {
        // Arrange — evento já em andamento (dataInicio no passado) pode ser editado
        // desde que a dataInicio enviada seja a MESMA já cadastrada.
        var dataOriginal = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var existing = new EventoPet { Id = "1", Titulo = "Evento em Andamento", DataInicio = dataOriginal };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.UpdateAsync("1", It.IsAny<EventoPet>())).ReturnsAsync((string _, EventoPet e) => e);

        var request = new EventoPetRequest
        {
            Titulo = "Evento em Andamento — Atualizado",
            Tipo = TipoEventoPetEnum.Outro,
            DataInicio = dataOriginal,
            EspecieAlvo = EspecieEnum.Todos
        };

        // Act
        var result = await _service.UpdateAsync("1", request);

        // Assert
        Assert.Equal("Evento em Andamento — Atualizado", result.Titulo);
    }

    [Fact]
    public async Task UpdateAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((EventoPet?)null);
        var request = new EventoPetRequest
        {
            Titulo = "X",
            Tipo = TipoEventoPetEnum.Outro,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            EspecieAlvo = EspecieEnum.Todos
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("id-invalido", request));
    }

    [Fact]
    public async Task DeleteAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.DeleteAsync("id-invalido")).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("id-invalido"));
    }
}
