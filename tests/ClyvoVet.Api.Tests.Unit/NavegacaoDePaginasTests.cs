using ClyvoVet.Api.Listagem;
using Microsoft.AspNetCore.Http;

namespace ClyvoVet.Api.Tests.Unit;

public class NavegacaoDePaginasTests
{
    private static HttpRequest Requisicao(string caminho, string query)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = caminho;
        contexto.Request.QueryString = new QueryString(query);
        return contexto.Request;
    }

    [Fact]
    public void Montar_PrimeiraPagina_TemFirstNextELastMasNaoPrev()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/api/v1/lembretes", "?animalId=a1"), 1, 10, 25);

        Assert.Equal("/api/v1/lembretes?animalId=a1&page=1&pageSize=10", links["self"]);
        Assert.Equal("/api/v1/lembretes?animalId=a1&page=1&pageSize=10", links["first"]);
        Assert.Equal("/api/v1/lembretes?animalId=a1&page=2&pageSize=10", links["next"]);
        Assert.Equal("/api/v1/lembretes?animalId=a1&page=3&pageSize=10", links["last"]);
        Assert.False(links.ContainsKey("prev"));
    }

    [Fact]
    public void Montar_PaginaDoMeio_TemPrevENext()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/api/v1/produtos", ""), 2, 10, 25);

        Assert.Equal("/api/v1/produtos?page=1&pageSize=10", links["prev"]);
        Assert.Equal("/api/v1/produtos?page=3&pageSize=10", links["next"]);
    }

    [Fact]
    public void Montar_UltimaPagina_TemPrevMasNaoNext()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/api/v1/produtos", ""), 3, 10, 25);

        Assert.True(links.ContainsKey("prev"));
        Assert.False(links.ContainsKey("next"));
    }

    [Fact]
    public void Montar_PaginaAlemDoFim_PrevApontaParaAUltimaPaginaReal()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/api/v1/produtos", ""), 9, 10, 25);

        Assert.Equal("/api/v1/produtos?page=3&pageSize=10", links["prev"]);
        Assert.False(links.ContainsKey("next"));
    }

    [Fact]
    public void Montar_ColecaoVazia_FirstELastSaoAPaginaUmESemPrevNext()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/api/v1/produtos", ""), 1, 10, 0);

        Assert.Equal(links["first"], links["last"]);
        Assert.False(links.ContainsKey("prev"));
        Assert.False(links.ContainsKey("next"));
    }

    [Fact]
    public void Montar_PreservaFiltrosEOrdenacaoESubstituiPageEPageSize()
    {
        var requisicao = Requisicao("/api/v1/lembretes", "?page=2&animalId=a1&ordenarPor=titulo&direcao=desc&pageSize=5");

        var links = NavegacaoDePaginas.Montar(requisicao, 2, 5, 12);

        Assert.Equal("/api/v1/lembretes?animalId=a1&ordenarPor=titulo&direcao=desc&page=3&pageSize=5", links["next"]);
    }

    [Fact]
    public void CabecalhoLink_PrimeiraPagina_ListaFirstNextELastNoFormatoDaRfc8288()
    {
        var links = NavegacaoDePaginas.Montar(Requisicao("/a", ""), 1, 10, 25);

        var cabecalho = NavegacaoDePaginas.CabecalhoLink(links);

        Assert.Equal(
            "</a?page=1&pageSize=10>; rel=\"first\", </a?page=2&pageSize=10>; rel=\"next\", </a?page=3&pageSize=10>; rel=\"last\"",
            cabecalho);
    }
}
