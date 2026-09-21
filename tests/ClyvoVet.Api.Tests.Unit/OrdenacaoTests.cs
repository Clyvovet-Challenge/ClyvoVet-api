using System.Linq.Expressions;
using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Repositories;

namespace ClyvoVet.Api.Tests.Unit;

public class OrdenacaoTests
{
    private record Linha(string Id, string Nome, DateTime Data);

    private static readonly Dictionary<string, Expression<Func<Linha, object?>>> Permitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["nome"] = l => l.Nome,
            ["data"] = l => l.Data,
        };

    private static readonly IQueryable<Linha> Linhas = new[]
    {
        new Linha("1", "banana", new DateTime(2026, 1, 3)),
        new Linha("2", "abacate", new DateTime(2026, 1, 1)),
        new Linha("3", "cereja", new DateTime(2026, 1, 2)),
    }.AsQueryable();

    private static string[] Ids(IEnumerable<Linha> linhas) => linhas.Select(l => l.Id).ToArray();

    private static IOrderedQueryable<Linha> Ordenar(IQueryable<Linha> origem, ConsultaPaginada pedido) =>
        Ordenacao.Aplicar(origem, pedido, Permitidos, q => q.OrderBy(l => l.Data), l => l.Id);

    [Fact]
    public void Aplicar_SemOrdenarPor_UsaAOrdemPadrao()
    {
        Assert.Equal(["2", "3", "1"], Ids(Ordenar(Linhas, new ConsultaPaginada(1, 10))));
    }

    [Fact]
    public void Aplicar_OrdenarPorNomeAsc_OrdenaDoMenorParaOMaior()
    {
        Assert.Equal(["2", "1", "3"], Ids(Ordenar(Linhas, new ConsultaPaginada(1, 10, "nome"))));
    }

    [Fact]
    public void Aplicar_OrdenarPorNomeDesc_OrdenaDoMaiorParaOMenor()
    {
        var pedido = new ConsultaPaginada(1, 10, "nome", DirecaoOrdenacao.Desc);
        Assert.Equal(["3", "1", "2"], Ids(Ordenar(Linhas, pedido)));
    }

    [Fact]
    public void Aplicar_CampoEmOutraCaixa_AindaOrdena()
    {
        Assert.Equal(["2", "1", "3"], Ids(Ordenar(Linhas, new ConsultaPaginada(1, 10, "NOME"))));
    }

    [Fact]
    public void Aplicar_CampoForaDaListaBranca_LancaBadRequestComOsCamposPermitidos()
    {
        // Arrange
        var pedido = new ConsultaPaginada(1, 10, "senha");

        // Act
        var erro = Assert.Throws<BadRequestException>(() => Ordenar(Linhas, pedido));

        // Assert
        Assert.Equal("Campo de ordenação 'senha' inválido. Permitidos: data, nome.", erro.Message);
    }

    [Fact]
    public void Aplicar_DirecaoSemOrdenarPor_EhIgnorada()
    {
        var pedido = new ConsultaPaginada(1, 10, null, DirecaoOrdenacao.Desc);
        Assert.Equal(["2", "3", "1"], Ids(Ordenar(Linhas, pedido)));
    }

    [Fact]
    public void Aplicar_ValoresIguais_DesempataPeloIdNasDuasDirecoes()
    {
        // Arrange: sem desempate, a ordem entre "a" e "b" seria indefinida e a paginação repetiria ou pularia linhas.
        var d = new DateTime(2026, 1, 1);
        var empatadas = new[] { new Linha("b", "x", d), new Linha("a", "x", d), new Linha("c", "y", d) }.AsQueryable();

        // Act & Assert
        Assert.Equal(["a", "b", "c"], Ids(Ordenar(empatadas, new ConsultaPaginada(1, 10, "nome"))));
        Assert.Equal(["c", "a", "b"], Ids(Ordenar(empatadas, new ConsultaPaginada(1, 10, "nome", DirecaoOrdenacao.Desc))));
    }
}
