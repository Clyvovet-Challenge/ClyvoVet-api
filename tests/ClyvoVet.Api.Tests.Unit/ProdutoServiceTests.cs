using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services;
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
    public async Task GetAllAsync_RepositorioRetornaProdutos_RetornaPaginaMapeadaComOTotal()
    {
        // Arrange
        var produtos = new List<Produto>
        {
            new() { Id = "1", Nome = "Ração", Categoria = CategoriaEnum.Racao, EspecieIndicada = EspecieEnum.Cachorro, Ativo = true }
        };
        var consulta = new ConsultaPaginada(1, 10);
        _repositoryMock
            // Árvore de expressão não aceita argumento opcional: `ativo` e `porteIndicado` vão explícitos.
            .Setup(r => r.GetAllAsync(consulta, null, null, null, null))
            .ReturnsAsync(new PaginaDeResultados<Produto>(produtos, 12, 1, 10));

        // Act
        var result = await _service.GetAllAsync(consulta, null, null);

        // Assert
        var item = Assert.Single(result.Itens);
        Assert.Equal("Ração", item.Nome);
        Assert.Equal(12, result.Total);
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
    public async Task UpdateAsync_ApagadoNoMeio_LancaNotFound()
    {
        // Arrange — outra requisição apaga o produto entre a leitura e a gravação.
        _repositoryMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(new Produto { Id = "1", Nome = "Ração" });
        _repositoryMock.Setup(r => r.UpdateAsync("1", It.IsAny<Produto>())).ReturnsAsync((Produto?)null);
        var request = new ProdutoRequest { Nome = "X", Categoria = CategoriaEnum.Outro, EspecieIndicada = EspecieEnum.Outro };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync("1", request));
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
