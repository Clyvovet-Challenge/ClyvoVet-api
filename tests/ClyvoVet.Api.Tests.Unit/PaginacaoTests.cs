using ClyvoVet.Api.Repositories;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O recorte de página.
///
/// <para>
/// Existe por um defeito verificado contra a pilha no ar: as quatro listagens da API
/// respondiam <b>500</b> a <c>?page=2147483647&amp;pageSize=100</c>. A conta
/// <c>(page - 1) * pageSize</c> era feita em <c>int</c> e estourava para <c>-200</c>,
/// e <c>Skip</c> negativo derruba a consulta. A validação dos controllers não pegava:
/// 2147483647 é maior que zero, e 100 está entre 1 e 100.
/// </para>
/// </summary>
public class PaginacaoTests
{
    private static IQueryable<int> Colecao(int quantos) =>
        Enumerable.Range(1, quantos).AsQueryable();

    [Fact]
    public void PrimeiraPagina_DevolveOComecoDaColecao()
    {
        var resultado = Paginacao.Aplicar(Colecao(50), page: 1, pageSize: 10).ToList();

        Assert.Equal(10, resultado.Count);
        Assert.Equal(1, resultado[0]);
    }

    [Fact]
    public void SegundaPagina_PulaAPrimeira()
    {
        var resultado = Paginacao.Aplicar(Colecao(50), page: 2, pageSize: 10).ToList();

        Assert.Equal(11, resultado[0]);
        Assert.Equal(20, resultado[^1]);
    }

    /// <summary>
    /// O caso que derrubava a API. 2147483646 × 100 = 214.748.364.600, que em 32 bits
    /// vira -200.
    /// </summary>
    [Fact]
    public void PageNoMaximoDoInt_NaoEstoura()
    {
        var resultado = Paginacao.Aplicar(Colecao(50), page: int.MaxValue, pageSize: 100).ToList();

        Assert.Empty(resultado);
    }

    [Theory]
    [InlineData(int.MaxValue, 100)]
    [InlineData(int.MaxValue, 2)]
    [InlineData(int.MaxValue - 1, 100)]
    [InlineData(999_999_999, 100)]
    [InlineData(21_474_837, 100)]
    public void QualquerPaginaAlem_DevolveVazioEmVezDeQuebrar(int page, int pageSize)
    {
        var resultado = Paginacao.Aplicar(Colecao(50), page, pageSize).ToList();

        Assert.Empty(resultado);
    }

    /// <summary>
    /// Os controllers recusam page &lt; 1, mas os repositórios também são chamados
    /// pelos BackgroundService e pelos testes — não podem depender disso.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void PageInvalida_NaoQuebra_ETrataComoAPrimeira(int page)
    {
        var resultado = Paginacao.Aplicar(Colecao(50), page, pageSize: 10).ToList();

        Assert.Equal(10, resultado.Count);
        Assert.Equal(1, resultado[0]);
    }

    /// <summary>
    /// pageSize negativo faria Take(-n), que quebra igual ao Skip negativo.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void PageSizeNegativo_DevolveVazioEmVezDeQuebrar(int pageSize)
    {
        Assert.Empty(Paginacao.Aplicar(Colecao(50), page: 1, pageSize).ToList());
    }

    [Fact]
    public void UltimaPaginaIncompleta_DevolveSoOQueSobrou()
    {
        var resultado = Paginacao.Aplicar(Colecao(25), page: 3, pageSize: 10).ToList();

        Assert.Equal(5, resultado.Count);
        Assert.Equal(21, resultado[0]);
    }
}
