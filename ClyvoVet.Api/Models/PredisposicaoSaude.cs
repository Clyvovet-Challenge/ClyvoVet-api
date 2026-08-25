using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Models;

public class PredisposicaoSaude
{
    public string Id { get; set; } = null!;
    public EspecieAnimalEnum Especie { get; set; }
    public string? Raca { get; set; }
    public decimal? IdadeMinimaAnos { get; set; }
    public string Doenca { get; set; } = null!;
    public string Recomendacao { get; set; } = null!;
    public string? FonteReferencia { get; set; }
    public DateTime CriadoEm { get; set; }
}
