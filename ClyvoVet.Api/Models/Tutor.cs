namespace ClyvoVet.Api.Models;

public class Tutor
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Cpf { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }

    public ICollection<Animal> Animais { get; set; } = new List<Animal>();
}
