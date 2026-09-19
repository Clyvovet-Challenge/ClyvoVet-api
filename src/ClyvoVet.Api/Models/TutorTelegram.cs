namespace ClyvoVet.Api.Models;

public class TutorTelegram
{
    public string Id { get; set; } = null!;
    public string TutorId { get; set; } = null!;
    public long ChatId { get; set; }
    public DateTime CriadoEm { get; set; }
}
