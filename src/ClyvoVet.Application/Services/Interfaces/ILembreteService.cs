using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Application.Services.Interfaces;

public interface ILembreteService
{
    Task<PaginaDeResultados<LembreteResponse>> GetAllAsync(ConsultaPaginada consulta, string? animalId, StatusLembreteEnum? status, TipoLembreteEnum? tipo, string? tutorId = null);
    Task<LembreteResponse> GetByIdAsync(string id);
    Task<LembreteResponse> CreateAsync(LembreteRequest request);
    Task<LembreteResponse> UpdateAsync(string id, LembreteRequest request);
    Task DeleteAsync(string id);
}
