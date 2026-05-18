using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IEventoPetRepository
{
    Task<IEnumerable<EventoPet>> GetAllAsync(int page, int pageSize, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo);
    Task<EventoPet?> GetByIdAsync(string id);
    Task<EventoPet> CreateAsync(EventoPet evento);
    Task<EventoPet?> UpdateAsync(string id, EventoPet evento);
    Task<bool> DeleteAsync(string id);
}
