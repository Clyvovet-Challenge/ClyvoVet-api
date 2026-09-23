using Microsoft.AspNetCore.Http;

namespace ClyvoVet.Api.Listagem;

/// <summary>
/// Decide se o cliente pediu o envelope (<c>Accept: application/vnd.clyvovet.hateoas+json</c>) em vez
/// do array de sempre. Olhar só se o tipo "aparece" no header não basta: um cliente que manda
/// <c>application/json, vnd…;q=0.9</c> prefere o JSON puro, e o <c>q</c> (a prioridade) diz isso.
/// </summary>
public static class PreferenciaDeEnvelope
{
    public const string TipoDeMidia = "application/vnd.clyvovet.hateoas+json";

    public static bool Pede(HttpRequest requisicao)
    {
        var aceitos = requisicao.GetTypedHeaders().Accept;

        var envelope = aceitos.FirstOrDefault(a => a.MediaType.Equals(TipoDeMidia, StringComparison.OrdinalIgnoreCase));
        if (envelope is null || (envelope.Quality ?? 1.0) <= 0)
            return false;

        var json = aceitos.FirstOrDefault(a => a.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase));
        return json is null || (envelope.Quality ?? 1.0) >= (json.Quality ?? 1.0);
    }
}
