using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Models;

public class EventoPet
{
    public string Id { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoEventoPetEnum Tipo { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public EspecieEnum EspecieAlvo { get; set; }
    public string? Organizador { get; set; }
    public bool Gratuito { get; set; }
    public string? LinkInscricao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
