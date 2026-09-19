using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ITutorTelegramRepository
{
    Task VincularAsync(string tutorId, long chatId);
    Task<long?> GetChatIdByTutorIdAsync(string tutorId);

    /// <summary>
    /// O vinculo inteiro, ou nulo quando nao existe.
    ///
    /// <para>
    /// Separado de <see cref="GetChatIdByTutorIdAsync"/> porque as duas
    /// perguntas sao diferentes: o envio quer "para onde mando", e a tela quer
    /// "existe, e desde quando".
    /// </para>
    /// </summary>
    Task<TutorTelegram?> ObterVinculoAsync(string tutorId);
    Task<string?> GetTutorIdByChatIdAsync(long chatId);
    Task<bool> DesvincularAsync(string tutorId);
}
