using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ILembreteRepository
{
    Task<IEnumerable<Lembrete>> GetAllAsync(int page, int pageSize, string? animalId, TipoLembreteEnum? tipo, StatusLembreteEnum? status);
    Task<Lembrete?> GetByIdAsync(string id);
    Task<Lembrete> CreateAsync(Lembrete lembrete);
    Task<Lembrete?> UpdateAsync(string id, Lembrete lembrete);
    Task<bool> DeleteAsync(string id);
}
