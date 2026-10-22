using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Application.Services.Interfaces;

public interface IEventoPetService
{
    Task<PaginaDeResultados<EventoPetResponse>> GetAllAsync(ConsultaPaginada consulta, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo);
    Task<EventoPetResponse> GetByIdAsync(string id);
    Task<EventoPetResponse> CreateAsync(EventoPetRequest request);
    Task<EventoPetResponse> UpdateAsync(string id, EventoPetRequest request);
    Task DeleteAsync(string id);
}
