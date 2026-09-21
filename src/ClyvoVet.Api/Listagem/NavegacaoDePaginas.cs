using System.Globalization;
using ClyvoVet.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ClyvoVet.Api.Listagem;

/// <summary>
/// Os links de navegação de uma listagem (<c>self</c>, <c>first</c>, <c>prev</c>, <c>next</c>,
/// <c>last</c>), preservando filtros, <c>ordenarPor</c> e <c>direcao</c> da requisição. Alimenta o
/// cabeçalho <c>Link</c> (RFC 8288) e o <c>_links</c> do envelope. <c>prev</c> e <c>next</c> só
/// existem quando há para onde ir.
/// </summary>
public static class NavegacaoDePaginas
{
    private static readonly string[] OrdemDoCabecalho = ["first", "prev", "next", "last"];

    public static IReadOnlyDictionary<string, string> Montar(HttpRequest requisicao, int page, int pageSize, int total)
    {
        var ultima = new PaginaDeResultados<int>([], total, page, pageSize).TotalDePaginas;

        var links = new Dictionary<string, string>
        {
            ["self"] = Caminho(requisicao, page, pageSize),
            ["first"] = Caminho(requisicao, 1, pageSize),
        };

        // Math.Min: quem pediu uma página além do fim volta para a última que existe, e não para
        // uma página vazia vizinha.
        if (page > 1) links["prev"] = Caminho(requisicao, Math.Min(page - 1, ultima), pageSize);
        if (page < ultima) links["next"] = Caminho(requisicao, page + 1, pageSize);
        links["last"] = Caminho(requisicao, ultima, pageSize);
        return links;
    }

    public static string CabecalhoLink(IReadOnlyDictionary<string, string> links) =>
        string.Join(", ", OrdemDoCabecalho.Where(links.ContainsKey).Select(rel => $"<{links[rel]}>; rel=\"{rel}\""));

    private static string Caminho(HttpRequest requisicao, int page, int pageSize)
    {
        var demais = requisicao.Query
            .Where(p => !p.Key.Equals("page", StringComparison.OrdinalIgnoreCase)
                     && !p.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            .SelectMany(p => p.Value.Select(v => new KeyValuePair<string, string>(p.Key, v ?? string.Empty)));

        var query = new QueryBuilder(demais)
        {
            { "page", page.ToString(CultureInfo.InvariantCulture) },
            { "pageSize", pageSize.ToString(CultureInfo.InvariantCulture) },
        };

        return $"{requisicao.PathBase}{requisicao.Path}{query.ToQueryString()}";
    }
}
