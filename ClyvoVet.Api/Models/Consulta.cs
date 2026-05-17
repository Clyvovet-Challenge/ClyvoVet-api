namespace ClyvoVet.Api.Models;

public class Consulta
{
    public string Id { get; set; } = null!;
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = null!;
    public string? Motivo { get; set; }
    public string? Observacoes { get; set; }
    public decimal Valor { get; set; }
    public DateTime CriadoEm { get; set; }

    public string AnimalId { get; set; } = null!;
    public Animal Animal { get; set; } = null!;

    public string VeterinarioId { get; set; } = null!;
    public Veterinario Veterinario { get; set; } = null!;
}
