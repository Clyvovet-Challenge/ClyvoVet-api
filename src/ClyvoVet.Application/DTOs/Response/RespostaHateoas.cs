using System.Text.Json.Serialization;

namespace ClyvoVet.Application.DTOs.Response;

/// <summary>Um link HATEOAS: para onde ir e com qual verbo.</summary>
public record Link(string Href, string Method);

/// <summary>
/// Base dos DTOs de resposta de consulta. <c>_links</c> é <b>aditivo</b>: acrescentar uma
/// propriedade a um objeto JSON não quebra quem só lê os campos que já conhecia (o app móvel).
/// É anulável e some do JSON quando nulo, então as respostas de POST e PUT — que usam o mesmo DTO,
/// mas não preenchem os links — saem idênticas às de antes.
/// </summary>
public abstract class RespostaHateoas
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, Link>? Links { get; set; }
}
