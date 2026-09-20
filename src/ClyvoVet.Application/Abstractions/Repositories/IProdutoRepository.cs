using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> GetAllAsync(int page, int pageSize, CategoriaEnum? categoria, EspecieEnum? especieIndicada, bool? ativo = null, PorteEnum? porteIndicado = null);
    Task<Produto?> GetByIdAsync(string id);
    Task<Produto> CreateAsync(Produto produto);
    Task<Produto?> UpdateAsync(string id, Produto produto);
    Task<bool> DeleteAsync(string id);
}
