using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IEventoPetRepository
{
    Task<PaginaDeResultados<EventoPet>> GetAllAsync(ConsultaPaginada consulta, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo);
    Task<EventoPet?> GetByIdAsync(string id);
    Task<EventoPet> CreateAsync(EventoPet evento);
    Task<EventoPet?> UpdateAsync(string id, EventoPet evento);
    Task<bool> DeleteAsync(string id);
}
