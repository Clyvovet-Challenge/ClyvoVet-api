using ClyvoVet.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class EventoPetRequest
{
    [Required]
    [MaxLength(300)]
    public string Titulo { get; set; } = null!;

    [MaxLength(2000)]
    public string? Descricao { get; set; }

    [Required]
    public TipoEventoPetEnum Tipo { get; set; }

    [MaxLength(300)]
    public string? Rua { get; set; }

    [MaxLength(20)]
    public string? Numero { get; set; }

    [MaxLength(200)]
    public string? Bairro { get; set; }

    [MaxLength(200)]
    public string? Cidade { get; set; }

    [MaxLength(2)]
    public string? Estado { get; set; }

    [MaxLength(9)]
    public string? Cep { get; set; }

    [Required]
    public DateOnly DataInicio { get; set; }

    public DateOnly? DataFim { get; set; }

    public EspecieEnum EspecieAlvo { get; set; } = EspecieEnum.Todos;

    [MaxLength(300)]
    public string? Organizador { get; set; }

    public bool Gratuito { get; set; } = true;

    [MaxLength(500)]
    public string? LinkInscricao { get; set; }

    public bool Ativo { get; set; } = true;
}
