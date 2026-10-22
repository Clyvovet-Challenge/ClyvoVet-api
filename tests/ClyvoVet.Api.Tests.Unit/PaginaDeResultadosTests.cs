using ClyvoVet.Application.Common;

namespace ClyvoVet.Api.Tests.Unit;

public class PaginaDeResultadosTests
{
    [Theory]
    [InlineData(0, 10, 1)]    // coleção vazia ainda tem uma página (vazia): o link "last" não pode ser 0
    [InlineData(1, 10, 1)]
    [InlineData(5, 2, 3)]
    [InlineData(4, 2, 2)]
    [InlineData(10, 0, 1)]    // pageSize inválido não pode dividir por zero
    public void TotalDePaginas_DadoTotalEPageSize_ArredondaParaCimaComMinimoDeUma(int total, int pageSize, int esperado)
    {
        // Arrange
        var pagina = new PaginaDeResultados<int>([], total, 1, pageSize);

        // Act & Assert
        Assert.Equal(esperado, pagina.TotalDePaginas);
    }

    [Fact]
    public void Mapear_ItensDaPagina_MapeiaOsItensEPreservaTotalEPosicao()
    {
        // Arrange
        var pagina = new PaginaDeResultados<int>([1, 2, 3], Total: 42, Page: 2, PageSize: 3);

        // Act
        var mapeada = pagina.Mapear(n => $"n{n}");

        // Assert
        Assert.Equal(["n1", "n2", "n3"], mapeada.Itens);
        Assert.Equal(42, mapeada.Total);
        Assert.Equal(2, mapeada.Page);
        Assert.Equal(3, mapeada.PageSize);
    }

    [Fact]
    public void ConsultaPaginada_SemOrdenacao_UsaAscESemCampo()
    {
        // Arrange & Act
        var consulta = new ConsultaPaginada(1, 10);

        // Assert
        Assert.Null(consulta.OrdenarPor);
        Assert.Equal(DirecaoOrdenacao.Asc, consulta.Direcao);
    }
}
