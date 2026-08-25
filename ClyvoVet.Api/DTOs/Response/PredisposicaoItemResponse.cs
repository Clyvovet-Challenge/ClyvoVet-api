namespace ClyvoVet.Api.DTOs.Response;

public class PredisposicaoItemResponse
{
    public string Doenca { get; set; } = null!;
    public string Recomendacao { get; set; } = null!;
    public decimal? IdadeMinimaAnos { get; set; }
    public string? FonteReferencia { get; set; }
}
