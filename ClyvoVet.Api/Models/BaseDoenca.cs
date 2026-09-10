namespace ClyvoVet.Api.Models;

/// <summary>
/// Uma linha da base agregada de doenças (t_clyvo_base_doencas, V15 do Flyway
/// da API Java): quantos casos e controles de uma doença existem nos datasets
/// de referência para uma espécie/raça. É o grounding do parecer de saúde
/// preditiva — a IA redige em cima destes números, e o fallback determinístico
/// ordena por eles. Somente leitura: o seed nasce na migration.
/// </summary>
public class BaseDoenca
{
    public string Id { get; set; } = null!;

    /// <summary>Vocabulário do catálogo da V14: CAO, GATO, ROEDOR, AVE, REPTIL.</summary>
    public string Especie { get; set; } = null!;

    /// <summary>O texto original do dataset ('labrador_retriever', 'Geospiza fuliginosa').</summary>
    public string RacaTexto { get; set; } = null!;

    /// <summary>Chave do catálogo (t_clyvo_raca.chave) quando a raça existe lá; NULL quando não.</summary>
    public string? RacaChave { get; set; }

    public string DoencaCodigo { get; set; } = null!;
    public string DoencaNome { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public int Casos { get; set; }
    public int Controles { get; set; }
    public string? Fonte { get; set; }
    public string? Doi { get; set; }
    public DateTime CriadoEm { get; set; }
}
