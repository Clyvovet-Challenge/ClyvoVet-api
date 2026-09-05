namespace ClyvoVet.Api.Models;

public class Tutor
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string? Cpf { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }

    public ICollection<Animal> Animais { get; set; } = new List<Animal>();
}
