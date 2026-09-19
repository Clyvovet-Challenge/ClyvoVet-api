using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ILembreteRepository
{
    /// <param name="tutorId">
    /// Quando informado, devolve apenas lembretes de animais deste tutor. Nulo
    /// significa sem recorte — e e o valor que os dois BackgroundService usam,
    /// porque eles nao tem chamador para recortar.
    /// </param>
    Task<IEnumerable<Lembrete>> GetAllAsync(int page, int pageSize, string? animalId, TipoLembreteEnum? tipo, StatusLembreteEnum? status, string? tutorId = null);
    Task<IEnumerable<Lembrete>> GetPendentesVencendoAsync(DateTime limite);
    Task<IEnumerable<Lembrete>> GetPendentesByTutorIdAsync(string tutorId);
    Task<Lembrete?> GetByIdAsync(string id);
    Task<Lembrete> CreateAsync(Lembrete lembrete);
    Task<Lembrete?> UpdateAsync(string id, Lembrete lembrete);
    Task<bool> DeleteAsync(string id);
}
