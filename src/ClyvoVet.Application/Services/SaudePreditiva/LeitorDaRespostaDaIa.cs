using System.Text.Json;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Services.SaudePreditiva;

/// <summary>
/// A resposta do LLM é entrada não confiável; esta classe é o filtro entre ela e o tutor.
/// </summary>
internal static class LeitorDaRespostaDaIa
{
    /// <summary>
    /// As mesmas opções servem para ler a IA e para gravar/ler o cache: o parecer
    /// em cache é exatamente o conteúdo que saiu daqui.
    /// </summary>
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// LLMs devolvem JSON com cercas de markdown, prefixos e afins — extrai o
    /// primeiro objeto e valida a forma. Qualquer coisa fora disso vira null,
    /// e null vira fallback: resposta ruim de IA nunca chega ao tutor.
    /// </summary>
    internal static ParecerConteudo? TentarLer(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        if (inicio < 0 || fim <= inicio)
            return null;

        try
        {
            var conteudo = JsonSerializer.Deserialize<ParecerConteudo>(texto[inicio..(fim + 1)], Json);
            if (conteudo is null || conteudo.Riscos.Count == 0)
                return null;
            if (conteudo.Riscos.Any(r => string.IsNullOrWhiteSpace(r.Doenca)))
                return null;
            return conteudo;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
