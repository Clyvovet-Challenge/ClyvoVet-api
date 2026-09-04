using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class LembreteServiceTests
{
    private readonly Mock<ILembreteRepository> _repositoryMock = new();
    private readonly Mock<IAnimalRepository> _animalRepositoryMock = new();
    private readonly LembreteService _service;

    public LembreteServiceTests()
    {
        _service = new LembreteService(_repositoryMock.Object, _animalRepositoryMock.Object);
    }

    private static Animal CriarAnimal(string id = "animal-1") => new()
    {
        Id = id,
        Nome = "Rex",
        Especie = "CACHORRO",
        TutorId = "tutor-1"
    };

    [Fact]
    public async Task CreateAsync_AnimalIdInexistente_LancaNotFoundException()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-invalido")).ReturnsAsync((Animal?)null);
        var request = new LembreteRequest
        {
            AnimalId = "animal-invalido",
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(5)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DataAgendadaNoPassado_LancaBadRequestException()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_ForcaStatusPendenteIndependenteDoEnviado()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());

        Lembrete? lembreteCriado = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Lembrete>()))
            .ReturnsAsync((Lembrete l) =>
            {
                l.Id = "novo-id";
                lembreteCriado = l;
                return l;
            });
        _repositoryMock
            .Setup(r => r.GetByIdAsync("novo-id"))
            .ReturnsAsync(() => lembreteCriado);

        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "Consulta de Rotina",
            Tipo = TipoLembreteEnum.Consulta,
            AgendadoEm = DateTime.UtcNow.AddDays(10),
            Status = StatusLembreteEnum.Enviado // deve ser ignorado
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(StatusLembreteEnum.Pendente, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Lembrete?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync("id-invalido"));
    }

    [Fact]
    public async Task GetAllAsync_RepositorioRetornaLembretes_RetornaListaMapeada()
    {
        // Arrange
        var lembretes = new List<Lembrete>
        {
            new() { Id = "1", AnimalId = "animal-1", Titulo = "Vacina Antirrábica", Tipo = TipoLembreteEnum.Vacina, Status = StatusLembreteEnum.Pendente }
        };
        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, "animal-1", TipoLembreteEnum.Vacina, StatusLembreteEnum.Pendente))
            .ReturnsAsync(lembretes);

        // Act
        var result = await _service.GetAllAsync(1, 10, "animal-1", StatusLembreteEnum.Pendente, TipoLembreteEnum.Vacina);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal("Vacina Antirrábica", item.Titulo);
    }

    [Fact]
    public async Task UpdateAsync_DadosValidos_AtualizaERetornaComNovosDados()
    {
        // Arrange
        var existing = new Lembrete { Id = "1", AnimalId = "animal-1", Titulo = "Original", Status = StatusLembreteEnum.Pendente };
        Lembrete? lembreteAtualizado = null;

        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(() => lembreteAtualizado ?? existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _repositoryMock
            .Setup(r => r.UpdateAsync("1", It.IsAny<Lembrete>()))
            .ReturnsAsync((string _, Lembrete l) =>
            {
                lembreteAtualizado = l;
                return l;
            });

        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "Título Atualizado",
            Tipo = TipoLembreteEnum.Consulta,
            AgendadoEm = DateTime.UtcNow.AddDays(10),
            Status = StatusLembreteEnum.Enviado
        };

        // Act
        var result = await _service.UpdateAsync("1", request);

        // Assert
        Assert.Equal("Título Atualizado", result.Titulo);
        Assert.Equal(StatusLembreteEnum.Pendente, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_LembreteJaEnviado_MantemStatusEnviadoAposEditarOutrosCampos()
    {
        // Arrange
        var existing = new Lembrete { Id = "1", AnimalId = "animal-1", Titulo = "Original", Status = StatusLembreteEnum.Enviado };
        Lembrete? lembreteAtualizado = null;

        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(() => lembreteAtualizado ?? existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _repositoryMock
            .Setup(r => r.UpdateAsync("1", It.IsAny<Lembrete>()))
            .ReturnsAsync((string _, Lembrete l) =>
            {
                lembreteAtualizado = l;
                return l;
            });

        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "Título Atualizado",
            Tipo = TipoLembreteEnum.Consulta,
            AgendadoEm = DateTime.UtcNow.AddDays(10)
        };

        // Act
        var result = await _service.UpdateAsync("1", request);

        // Assert
        Assert.Equal("Título Atualizado", result.Titulo);
        Assert.Equal(StatusLembreteEnum.Enviado, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Lembrete?)null);
        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "X",
            Tipo = TipoLembreteEnum.Outro,
            AgendadoEm = DateTime.UtcNow.AddDays(2)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("id-invalido", request));
        _animalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AnimalIdInexistente_LancaNotFoundException()
    {
        // Arrange
        var existing = new Lembrete { Id = "1", AnimalId = "animal-1", Titulo = "Original" };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-invalido")).ReturnsAsync((Animal?)null);

        var request = new LembreteRequest
        {
            AnimalId = "animal-invalido",
            Titulo = "Atualizado",
            Tipo = TipoLembreteEnum.Outro,
            AgendadoEm = DateTime.UtcNow.AddDays(2)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("1", request));
    }

    [Fact]
    public async Task UpdateAsync_DataAgendadaNoPassado_LancaBadRequestException()
    {
        // Arrange
        var existing = new Lembrete { Id = "1", AnimalId = "animal-1", Titulo = "Original" };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());

        var request = new LembreteRequest
        {
            AnimalId = "animal-1",
            Titulo = "Atualizado",
            Tipo = TipoLembreteEnum.Outro,
            AgendadoEm = DateTime.UtcNow.AddDays(-3)
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync("1", request));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.DeleteAsync("id-invalido")).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("id-invalido"));
    }

    [Fact]
    public async Task DeleteAsync_IdExistente_NaoLancaExcecao()
    {
        // Arrange
        _repositoryMock.Setup(r => r.DeleteAsync("1")).ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(() => _service.DeleteAsync("1"));

        // Assert
        Assert.Null(exception);
    }
}
