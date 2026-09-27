using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface ILembreteRepository
{
    /// <param name="tutorId">
    /// Quando informado, devolve apenas lembretes de animais deste tutor. Nulo
    /// significa sem recorte — e e o valor que os dois BackgroundService usam,
    /// porque eles nao tem chamador para recortar.
    /// </param>
    Task<PaginaDeResultados<Lembrete>> GetAllAsync(ConsultaPaginada consulta, string? animalId, TipoLembreteEnum? tipo, StatusLembreteEnum? status, string? tutorId = null);
    /// <summary>
    /// Pendentes que vencem até <paramref name="limite"/>, só de tutores com Telegram vinculado:
    /// sem canal não há o que fazer com eles, e varrê-los a cada minuto era girar em falso.
    /// </summary>
    Task<IEnumerable<Lembrete>> GetPendentesVencendoAsync(DateTime limite);

    /// <summary>Os que a varredura deixa de fora por falta de Telegram, para o log contar.</summary>
    Task<int> ContarPendentesSemTelegramAsync(DateTime limite);
    Task<IEnumerable<Lembrete>> GetPendentesByTutorIdAsync(string tutorId);
    Task<Lembrete?> GetByIdAsync(string id);
    Task<Lembrete> CreateAsync(Lembrete lembrete);
    Task<Lembrete?> UpdateAsync(string id, Lembrete lembrete);
    Task<bool> DeleteAsync(string id);
}
