using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IProdutoRepository
{
    Task<PaginaDeResultados<Produto>> GetAllAsync(ConsultaPaginada consulta, CategoriaEnum? categoria, EspecieEnum? especieIndicada, bool? ativo = null, PorteEnum? porteIndicado = null);
    Task<Produto?> GetByIdAsync(string id);
    Task<Produto> CreateAsync(Produto produto);
    Task<Produto?> UpdateAsync(string id, Produto produto);
    Task<bool> DeleteAsync(string id);
}
