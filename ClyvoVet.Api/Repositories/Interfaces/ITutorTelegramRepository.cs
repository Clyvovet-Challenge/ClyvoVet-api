namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ITutorTelegramRepository
{
    Task VincularAsync(string tutorId, long chatId);
    Task<long?> GetChatIdByTutorIdAsync(string tutorId);
    Task<string?> GetTutorIdByChatIdAsync(long chatId);
}
