using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> GetAllAsync(int page, int pageSize, CategoriaEnum? categoria, EspecieEnum? especieIndicada);
    Task<Produto?> GetByIdAsync(string id);
    Task<Produto> CreateAsync(Produto produto);
    Task<Produto?> UpdateAsync(string id, Produto produto);
    Task<bool> DeleteAsync(string id);
}
