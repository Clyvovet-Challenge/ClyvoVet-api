namespace ClyvoVet.Api.DTOs.Response;

public class WidgetSaudePreditivaResponse
{
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string Especie { get; set; } = null!;
    public string? Raca { get; set; }
    public decimal? IdadeAnos { get; set; }
    public bool SugerirAgendamentoConsulta { get; set; }
    public List<PredisposicaoItemResponse> Predisposicoes { get; set; } = [];
}
