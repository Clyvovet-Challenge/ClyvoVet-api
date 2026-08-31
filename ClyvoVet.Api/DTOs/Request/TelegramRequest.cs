using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class TelegramRequest
{
    [Required]
    public long ChatId { get; set; }

    [Required]
    [MaxLength(4096)]
    public string Mensagem { get; set; } = null!;
}
