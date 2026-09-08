using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface ISugestaoProdutoRepository
{
    /// <param name="tutorId">
    /// Quando informado, devolve apenas sugestoes de animais deste tutor. Nulo
    /// significa sem recorte.
    /// </param>
    Task<IEnumerable<SugestaoProduto>> GetAllAsync(int page, int pageSize, string? animalId, string? tutorId = null);
    Task<SugestaoProduto?> GetByIdAsync(string id);
    Task<SugestaoProduto> CreateAsync(SugestaoProduto sugestao);
    Task<SugestaoProduto?> UpdateAsync(string id, SugestaoProduto sugestao);
    Task<bool> DeleteAsync(string id);
}
