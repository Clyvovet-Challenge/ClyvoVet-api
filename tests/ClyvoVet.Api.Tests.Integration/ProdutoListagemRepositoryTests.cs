using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class ProdutoListagemRepositoryTests
{
    private static AppDbContext CriarContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"ProdutoListagemDb-{Guid.NewGuid()}").Options);

    private static Produto Novo(string nome, decimal preco) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Nome = nome,
        Categoria = CategoriaEnum.Racao,
        EspecieIndicada = EspecieEnum.Cachorro,
        Preco = preco,
        Ativo = true,
        CriadoEm = DateTime.UtcNow,
    };

    private static string[] Nomes(PaginaDeResultados<Produto> pagina) => pagina.Itens.Select(p => p.Nome).ToArray();

    private static Task<PaginaDeResultados<Produto>> Listar(AppDbContext contexto, ConsultaPaginada consulta) =>
        new ProdutoRepository(contexto).GetAllAsync(consulta, null, null, null, null);

    [Fact]
    public async Task GetAllAsync_SemOrdenarPor_MantemAOrdemPorNome()
    {
        // Arrange
        using var contexto = CriarContexto();
        contexto.Produtos.AddRange(Novo("Ração C", 30), Novo("Ração A", 10), Novo("Ração B", 20));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await Listar(contexto, new ConsultaPaginada(1, 10));

        // Assert
        Assert.Equal(["Ração A", "Ração B", "Ração C"], Nomes(pagina));
    }

    [Fact]
    public async Task GetAllAsync_OrdenarPorPrecoDesc_OrdenaDoMaisCaroParaOMaisBarato()
    {
        // Arrange
        using var contexto = CriarContexto();
        contexto.Produtos.AddRange(Novo("A", 10), Novo("B", 30), Novo("C", 20));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await Listar(contexto, new ConsultaPaginada(1, 10, "preco", DirecaoOrdenacao.Desc));

        // Assert
        Assert.Equal(["B", "C", "A"], Nomes(pagina));
    }

    [Fact]
    public async Task GetAllAsync_CampoForaDaListaBranca_LancaBadRequest()
    {
        using var contexto = CriarContexto();

        var erro = await Assert.ThrowsAsync<BadRequestException>(() =>
            Listar(contexto, new ConsultaPaginada(1, 10, "senha")));

        Assert.StartsWith("Campo de ordenação 'senha' inválido. Permitidos: ", erro.Message);
    }

    [Fact]
    public async Task GetAllAsync_TotalIgnoraORecorteDePaginaEContaSoOFiltrado()
    {
        // Arrange
        using var contexto = CriarContexto();
        for (var i = 1; i <= 5; i++) contexto.Produtos.Add(Novo($"P{i}", i));
        var inativo = Novo("Inativo", 1);
        inativo.Ativo = false;
        contexto.Produtos.Add(inativo);
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new ProdutoRepository(contexto)
            .GetAllAsync(new ConsultaPaginada(2, 2), null, null, ativo: true, porteIndicado: null);

        // Assert
        Assert.Equal(2, pagina.Itens.Count);
        Assert.Equal(5, pagina.Total);
    }
}
