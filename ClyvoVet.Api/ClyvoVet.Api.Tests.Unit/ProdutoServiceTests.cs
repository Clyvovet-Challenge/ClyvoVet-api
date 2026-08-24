using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class ProdutoServiceTests
{
    private readonly Mock<IProdutoRepository> _repositoryMock = new();
    private readonly ProdutoService _service;

    public ProdutoServiceTests()
    {
        _service = new ProdutoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_RepositorioRetornaProdutos_RetornaListaMapeada()
    {
        // Arrange
        var produtos = new List<Produto>
        {
            new() { Id = "1", Nome = "Ração", Categoria = CategoriaEnum.Racao, EspecieIndicada = EspecieEnum.Cachorro, Ativo = true }
        };
        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, null, null))
            .ReturnsAsync(produtos);

        // Act
        var result = await _service.GetAllAsync(1, 10, null, null);

        // Assert
        var resultList = Assert.Single(result);
        Assert.Equal("Ração", resultList.Nome);
    }

    [Fact]
    public async Task GetByIdAsync_IdExistente_RetornaProdutoMapeado()
    {
        // Arrange
        var produto = new Produto { Id = "1", Nome = "Ração", Categoria = CategoriaEnum.Racao, EspecieIndicada = EspecieEnum.Cachorro };
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(produto);

        // Act
        var result = await _service.GetByIdAsync("1");

        // Assert
        Assert.Equal("1", result.Id);
        Assert.Equal("Ração", result.Nome);
    }

    [Fact]
    public async Task GetByIdAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Produto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync("id-invalido"));
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaProdutoCriado()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Shampoo",
            Categoria = CategoriaEnum.Acessorio,
            Preco = 30m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Produto>()))
            .ReturnsAsync((Produto p) =>
            {
                p.Id = "novo-id";
                return p;
            });

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("novo-id", result.Id);
        Assert.Equal(request.Nome, result.Nome);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Produto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_IdInexistente_LancaNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Produto?)null);
        var request = new ProdutoRequest { Nome = "X", Categoria = CategoriaEnum.Outro, EspecieIndicada = EspecieEnum.Outro };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("id-invalido", request));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Produto>()), Times.Never);
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
