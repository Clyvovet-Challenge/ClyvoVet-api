using System.Text.Json.Serialization;
using ClyvoVet.Application.DTOs.Response;

namespace ClyvoVet.Api.Listagem;

/// <summary>
/// O corpo da listagem quando o cliente pede <c>application/vnd.clyvovet.hateoas+json</c>: os itens
/// e, ao lado, onde a página está e para onde ir. Quem não pedir isso não recebe nada diferente.
/// </summary>
public record EnvelopeHateoas<T>(
    IReadOnlyList<T> Itens,
    int Page,
    int PageSize,
    int Total,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, Link> Links);
