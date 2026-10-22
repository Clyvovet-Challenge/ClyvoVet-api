using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class SugestaoProdutoServiceTests
{
    private readonly Mock<ISugestaoProdutoRepository> _repositoryMock = new();
    private readonly Mock<IAnimalRepository> _animalRepositoryMock = new();
    private readonly Mock<IProdutoRepository> _produtoRepositoryMock = new();
    private readonly SugestaoProdutoService _service;

    public SugestaoProdutoServiceTests()
    {
        _service = new SugestaoProdutoService(_repositoryMock.Object, _animalRepositoryMock.Object, _produtoRepositoryMock.Object);
    }

    private static Animal CriarAnimal() => new() { Id = "animal-1", Nome = "Rex", Especie = "CACHORRO", TutorId = "tutor-1" };
    private static Produto CriarProduto() => new() { Id = "produto-1", Nome = "Ração" };

    [Fact]
    public async Task GetAllAsync_RepositorioRetornaSugestoes_RetornaPaginaMapeadaComOTotal()
    {
        // Arrange
        var sugestoes = new List<SugestaoProduto>
        {
            new() { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1", Justificativa = "Recomendado pelo veterinário", Ativo = true }
        };
        var consulta = new ConsultaPaginada(1, 10);
        _repositoryMock
            // Os dois nulls no fim são o recorte por tutor ausente (Api:EscopoPorTutor desligado) e o
            // filtro `ativo`. Árvore de expressão não aceita argumento opcional, então vão explícitos.
            .Setup(r => r.GetAllAsync(consulta, "animal-1", null, null))
            .ReturnsAsync(new PaginaDeResultados<SugestaoProduto>(sugestoes, 3, 1, 10));

        // Act
        var result = await _service.GetAllAsync(consulta, "animal-1");

        // Assert
        var item = Assert.Single(result.Itens);
        Assert.Equal("Recomendado pelo veterinário", item.Justificativa);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetByIdAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((SugestaoProduto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync("id-invalido"));
    }

    [Fact]
    public async Task CreateAsync_AnimalIdInexistente_LancaNotFoundException()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-invalido")).ReturnsAsync((Animal?)null);
        var request = new SugestaoProdutoRequest { AnimalId = "animal-invalido", ProdutoId = "produto-1" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
        _produtoRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ProdutoIdInexistente_LancaNotFoundException()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-invalido")).ReturnsAsync((Produto?)null);
        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-invalido" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<SugestaoProduto>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SemDataInformada_UsaDataDeHoje()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-1")).ReturnsAsync(CriarProduto());

        SugestaoProduto? criada = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<SugestaoProduto>()))
            .ReturnsAsync((SugestaoProduto s) =>
            {
                s.Id = "novo-id";
                criada = s;
                return s;
            });
        _repositoryMock.Setup(r => r.GetByIdAsync("novo-id")).ReturnsAsync(() => criada);

        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-1", DataSugestao = null };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        // "Hoje" é o de Brasília: entre 21h e meia-noite o UTC já está no dia seguinte.
        Assert.Equal(HorarioDeBrasilia.Hoje(), result.DataSugestao);
    }

    [Fact]
    public async Task UpdateAsync_DadosValidos_AtualizaERetornaComNovosDados()
    {
        // Arrange
        var existing = new SugestaoProduto { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1", Justificativa = "Original", Ativo = true };
        SugestaoProduto? atualizada = null;

        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(() => atualizada ?? existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-1")).ReturnsAsync(CriarProduto());
        _repositoryMock
            .Setup(r => r.UpdateAsync("1", It.IsAny<SugestaoProduto>()))
            .ReturnsAsync((string _, SugestaoProduto s) =>
            {
                atualizada = s;
                return s;
            });

        var request = new SugestaoProdutoRequest
        {
            AnimalId = "animal-1",
            ProdutoId = "produto-1",
            Justificativa = "Justificativa atualizada após reavaliação",
            Ativo = false
        };

        // Act
        var result = await _service.UpdateAsync("1", request);

        // Assert
        Assert.Equal("Justificativa atualizada após reavaliação", result.Justificativa);
        Assert.False(result.Ativo);
    }

    [Fact]
    public async Task UpdateAsync_SemDataSugestao_MantemDataOriginal()
    {
        // Arrange
        var dataOriginal = new DateOnly(2026, 1, 15);
        var existing = new SugestaoProduto { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1", DataSugestao = dataOriginal, Ativo = true };
        SugestaoProduto? gravada = null;

        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(() => gravada ?? existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-1")).ReturnsAsync(CriarProduto());
        _repositoryMock
            .Setup(r => r.UpdateAsync("1", It.IsAny<SugestaoProduto>()))
            .ReturnsAsync((string _, SugestaoProduto s) =>
            {
                gravada = s;
                return s;
            });

        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-1", DataSugestao = null };

        // Act
        var result = await _service.UpdateAsync("1", request);

        // Assert
        Assert.Equal(dataOriginal, gravada!.DataSugestao);
        Assert.Equal(dataOriginal, result.DataSugestao);
    }

    [Fact]
    public async Task UpdateAsync_ApagadoNoMeio_LancaNotFound()
    {
        // Arrange — a sugestão existe na primeira leitura e some antes da releitura.
        var existing = new SugestaoProduto { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1", Ativo = true };
        _repositoryMock.SetupSequence(r => r.GetByIdAsync("1"))
            .ReturnsAsync(existing)
            .ReturnsAsync((SugestaoProduto?)null);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-1")).ReturnsAsync(CriarProduto());
        _repositoryMock.Setup(r => r.UpdateAsync("1", It.IsAny<SugestaoProduto>())).ReturnsAsync((SugestaoProduto?)null);
        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-1" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("1", request));
    }

    [Fact]
    public async Task UpdateAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((SugestaoProduto?)null);
        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-1" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("id-invalido", request));
        _animalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AnimalIdInexistente_LancaNotFoundException()
    {
        // Arrange
        var existing = new SugestaoProduto { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1" };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-invalido")).ReturnsAsync((Animal?)null);

        var request = new SugestaoProdutoRequest { AnimalId = "animal-invalido", ProdutoId = "produto-1" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("1", request));
        _produtoRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ProdutoIdInexistente_LancaNotFoundException()
    {
        // Arrange
        var existing = new SugestaoProduto { Id = "1", AnimalId = "animal-1", ProdutoId = "produto-1" };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(CriarAnimal());
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync("produto-invalido")).ReturnsAsync((Produto?)null);

        var request = new SugestaoProdutoRequest { AnimalId = "animal-1", ProdutoId = "produto-invalido" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("1", request));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<SugestaoProduto>()), Times.Never);
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
