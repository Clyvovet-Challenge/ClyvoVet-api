namespace ClyvoVet.Api.Models;

public class Animal
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Especie { get; set; } = null!;
    public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public bool Castrado { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }

    public string TutorId { get; set; } = null!;
    public Tutor Tutor { get; set; } = null!;

    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}
