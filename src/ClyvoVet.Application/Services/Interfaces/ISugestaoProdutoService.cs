using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;

namespace ClyvoVet.Application.Services.Interfaces;

public interface ISugestaoProdutoService
{
    Task<PaginaDeResultados<SugestaoProdutoResponse>> GetAllAsync(ConsultaPaginada consulta, string? animalId, string? tutorId = null, bool? ativo = null);
    Task<SugestaoProdutoResponse> GetByIdAsync(string id);
    Task<SugestaoProdutoResponse> CreateAsync(SugestaoProdutoRequest request);
    Task<SugestaoProdutoResponse> UpdateAsync(string id, SugestaoProdutoRequest request);
    Task DeleteAsync(string id);
}
