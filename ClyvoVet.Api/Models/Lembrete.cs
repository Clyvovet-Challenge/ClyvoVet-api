using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Models;

public class Lembrete
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoLembreteEnum Tipo { get; set; }
    public DateTime AgendadoEm { get; set; }
    public bool Recorrente { get; set; }
    public StatusLembreteEnum Status { get; set; }
    public DateTime CriadoEm { get; set; }
}
