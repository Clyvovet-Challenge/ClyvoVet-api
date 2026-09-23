using System.Globalization;
using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Response;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Listagem;

public static class ListagemExtensions
{
    /// <summary>
    /// A resposta de uma listagem: por padrão o array de sempre (é o que o app móvel lê), com a
    /// navegação nos cabeçalhos <c>X-Total-Count</c> e <c>Link</c>; com o <c>Accept</c> do envelope,
    /// o envelope.
    /// </summary>
    public static IActionResult RespostaDeListagem<T>(this ControllerBase controller, PaginaDeResultados<T> pagina)
    {
        var links = NavegacaoDePaginas.Montar(controller.Request, pagina.Page, pagina.PageSize, pagina.Total);

        controller.Response.Headers["X-Total-Count"] = pagina.Total.ToString(CultureInfo.InvariantCulture);
        controller.Response.Headers["Link"] = NavegacaoDePaginas.CabecalhoLink(links);

        if (PreferenciaDeEnvelope.Pede(controller.Request))
        {
            var linksDoEnvelope = links.ToDictionary(par => par.Key, par => new Link(par.Value, "GET"));
            var envelope = new EnvelopeHateoas<T>(pagina.Itens, pagina.Page, pagina.PageSize, pagina.Total, linksDoEnvelope);

            // JsonResult, e não ObjectResult: o [Produces("application/json")] dos controllers
            // sobrescreve o ContentTypes de um ObjectResult, e o envelope sairia como application/json.
            return new JsonResult(envelope) { ContentType = PreferenciaDeEnvelope.TipoDeMidia };
        }

        return controller.Ok(pagina.Itens);
    }
}
