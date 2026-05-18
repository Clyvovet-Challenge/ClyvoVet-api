using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Services.Interfaces;

public interface ILembreteService
{
    Task<IEnumerable<LembreteResponse>> GetAllAsync(int page, int pageSize, string? animalId, StatusLembreteEnum? status, TipoLembreteEnum? tipo);
    Task<LembreteResponse> GetByIdAsync(string id);
    Task<LembreteResponse> CreateAsync(LembreteRequest request);
    Task<LembreteResponse> UpdateAsync(string id, LembreteRequest request);
    Task DeleteAsync(string id);
}
