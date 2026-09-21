namespace ClyvoVet.Application.Common;

/// <summary>
/// Uma página de uma listagem, já com o <b>total</b> de itens. O total é o que permite montar o
/// link <c>last</c> e o cabeçalho <c>X-Total-Count</c>. O serviço devolve isto; quem decide a
/// representação (array, envelope) é o controller.
/// </summary>
public record PaginaDeResultados<T>(IReadOnlyList<T> Itens, int Total, int Page, int PageSize)
{
    /// <summary>Nunca menor que 1: uma coleção vazia tem uma página, vazia, e não zero páginas.</summary>
    public int TotalDePaginas =>
        PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public PaginaDeResultados<TSaida> Mapear<TSaida>(Func<T, TSaida> mapa) =>
        new(Itens.Select(mapa).ToList(), Total, Page, PageSize);
}
