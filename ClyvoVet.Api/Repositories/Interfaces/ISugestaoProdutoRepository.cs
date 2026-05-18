using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ISugestaoProdutoRepository
{
    Task<IEnumerable<SugestaoProduto>> GetAllAsync(int page, int pageSize, string? animalId);
    Task<SugestaoProduto?> GetByIdAsync(string id);
    Task<SugestaoProduto> CreateAsync(SugestaoProduto sugestao);
    Task<SugestaoProduto?> UpdateAsync(string id, SugestaoProduto sugestao);
    Task<bool> DeleteAsync(string id);
}
