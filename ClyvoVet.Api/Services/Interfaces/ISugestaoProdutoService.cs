using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;

namespace ClyvoVet.Api.Services.Interfaces;

public interface ISugestaoProdutoService
{
    Task<IEnumerable<SugestaoProdutoResponse>> GetAllAsync(int page, int pageSize, string? animalId);
    Task<SugestaoProdutoResponse> GetByIdAsync(string id);
    Task<SugestaoProdutoResponse> CreateAsync(SugestaoProdutoRequest request);
    Task<SugestaoProdutoResponse> UpdateAsync(string id, SugestaoProdutoRequest request);
    Task DeleteAsync(string id);
}
