using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Services.Interfaces;

public interface IProdutoService
{
    Task<IEnumerable<ProdutoResponse>> GetAllAsync(int page, int pageSize, CategoriaEnum? categoria, EspecieEnum? especieIndicada);
    Task<ProdutoResponse> GetByIdAsync(string id);
    Task<ProdutoResponse> CreateAsync(ProdutoRequest request);
    Task<ProdutoResponse> UpdateAsync(string id, ProdutoRequest request);
    Task DeleteAsync(string id);
}
