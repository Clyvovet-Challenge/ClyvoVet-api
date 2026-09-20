using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Domain.Entities;

public class PredisposicaoSaude
{
    public string Id { get; set; } = null!;
    public EspecieEnum Especie { get; set; }
    public string? Raca { get; set; }
    public decimal? IdadeMinimaAnos { get; set; }
    public string Doenca { get; set; } = null!;
    public string Recomendacao { get; set; } = null!;
    public string? FonteReferencia { get; set; }
    public DateTime CriadoEm { get; set; }
}
