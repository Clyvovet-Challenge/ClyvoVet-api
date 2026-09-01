namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ITutorTelegramRepository
{
    Task VincularAsync(string tutorId, long chatId);
}
