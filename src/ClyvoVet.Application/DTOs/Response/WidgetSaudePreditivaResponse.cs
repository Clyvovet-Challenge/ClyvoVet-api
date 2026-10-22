namespace ClyvoVet.Application.DTOs.Response;

public class WidgetSaudePreditivaResponse : RespostaHateoas
{
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string Especie { get; set; } = null!;
    public string? Raca { get; set; }
    public decimal? IdadeAnos { get; set; }
    public bool SugerirAgendamentoConsulta { get; set; }
    public List<PredisposicaoItemResponse> Predisposicoes { get; set; } = [];
}
