using ClyvoVet.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class LembreteRequest
{
    [Required]
    public string AnimalId { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = null!;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [Required]
    public TipoLembreteEnum Tipo { get; set; }

    [Required]
    public DateTime AgendadoEm { get; set; }

    public bool Recorrente { get; set; } = false;

    public StatusLembreteEnum Status { get; set; } = StatusLembreteEnum.Pendente;
}
