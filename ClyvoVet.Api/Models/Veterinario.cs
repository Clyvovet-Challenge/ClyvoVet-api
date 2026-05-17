namespace ClyvoVet.Api.Models;

public class Veterinario
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Crmv { get; set; } = null!;
    public string? Email { get; set; }
    public string? Especialidade { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }

    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}
