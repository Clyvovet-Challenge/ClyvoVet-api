namespace ClyvoVet.Application.DTOs.Response;

public class TelegramLinkResponse : RespostaHateoas
{
    public string Link { get; set; } = null!;
}
