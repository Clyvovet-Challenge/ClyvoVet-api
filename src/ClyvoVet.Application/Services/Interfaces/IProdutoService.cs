using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Application.Services.Interfaces;

public interface IProdutoService
{
    Task<PaginaDeResultados<ProdutoResponse>> GetAllAsync(ConsultaPaginada consulta, CategoriaEnum? categoria, EspecieEnum? especieIndicada, bool? ativo = null, PorteEnum? porteIndicado = null);
    Task<ProdutoResponse> GetByIdAsync(string id);
    Task<ProdutoResponse> CreateAsync(ProdutoRequest request);
    Task<ProdutoResponse> UpdateAsync(string id, ProdutoRequest request);
    Task DeleteAsync(string id);
}
