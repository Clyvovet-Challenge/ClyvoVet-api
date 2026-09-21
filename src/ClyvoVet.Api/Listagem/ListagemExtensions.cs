using System.Globalization;
using ClyvoVet.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Listagem;

public static class ListagemExtensions
{
    /// <summary>
    /// A resposta padrão de uma listagem: o corpo continua sendo o <b>array</b> de sempre (é o que o
    /// app móvel lê), e a navegação vai nos cabeçalhos <c>X-Total-Count</c> e <c>Link</c>.
    /// </summary>
    public static IActionResult RespostaDeListagem<T>(this ControllerBase controller, PaginaDeResultados<T> pagina)
    {
        var links = NavegacaoDePaginas.Montar(controller.Request, pagina.Page, pagina.PageSize, pagina.Total);

        controller.Response.Headers["X-Total-Count"] = pagina.Total.ToString(CultureInfo.InvariantCulture);
        controller.Response.Headers["Link"] = NavegacaoDePaginas.CabecalhoLink(links);

        return controller.Ok(pagina.Itens);
    }
}
