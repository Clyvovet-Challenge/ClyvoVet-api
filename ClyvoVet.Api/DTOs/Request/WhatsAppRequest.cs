using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class WhatsAppRequest
{
    [Required]
    [MaxLength(20)]
    public string Telefone { get; set; } = null!;

    [Required]
    [MaxLength(1600)]
    public string Mensagem { get; set; } = null!;
}
