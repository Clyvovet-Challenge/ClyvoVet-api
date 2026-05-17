using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.DTOs.Response;

public class LembreteResponse
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoLembreteEnum Tipo { get; set; }
    public DateTime AgendadoEm { get; set; }
    public bool Recorrente { get; set; }
    public StatusLembreteEnum Status { get; set; }
    public DateTime CriadoEm { get; set; }
}
