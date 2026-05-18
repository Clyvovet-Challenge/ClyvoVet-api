using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Services.Interfaces;

public interface IEventoPetService
{
    Task<IEnumerable<EventoPetResponse>> GetAllAsync(int page, int pageSize, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo);
    Task<EventoPetResponse> GetByIdAsync(string id);
    Task<EventoPetResponse> CreateAsync(EventoPetRequest request);
    Task<EventoPetResponse> UpdateAsync(string id, EventoPetRequest request);
    Task DeleteAsync(string id);
}
